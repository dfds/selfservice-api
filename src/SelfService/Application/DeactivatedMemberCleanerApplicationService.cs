using System.Text;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Application;

public class DeactivatedMemberCleanerApplicationService : IDeactivatedMemberCleanerApplicationService
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;
    private readonly IMissingMemberRepository _missingMemberRepository;
    private readonly IRbacPermissionGrantRepository _rbacPermissionGrantRepository;
    private readonly IRbacRoleGrantRepository _rbacRoleGrantRepository;
    private readonly IRbacGroupMemberRepository _rbacGroupMemberRepository;

    private readonly ILogger<DeactivatedMemberCleanerApplicationService> _logger;

    private readonly StringBuilder _sb = new();
    private const int GracePeriodDays = 7;

    public DeactivatedMemberCleanerApplicationService(
        ILogger<DeactivatedMemberCleanerApplicationService> logger,
        IMembershipRepository membershipRepository,
        IMemberRepository memberRepository,
        IMembershipApplicationRepository membershipApplicationRepository,
        IMissingMemberRepository missingMemberRepository,
        IRbacPermissionGrantRepository rbacPermissionGrantRepository,
        IRbacRoleGrantRepository rbacRoleGrantRepository,
        IRbacGroupMemberRepository rbacGroupMemberRepository
    )
    {
        _membershipRepository = membershipRepository;
        _memberRepository = memberRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _missingMemberRepository = missingMemberRepository;
        _rbacPermissionGrantRepository = rbacPermissionGrantRepository;
        _rbacRoleGrantRepository = rbacRoleGrantRepository;
        _rbacGroupMemberRepository = rbacGroupMemberRepository;
        _logger = logger;
    }

    private string ToIdStringList(List<Member> members)
    {
        _sb.Clear();

        foreach (var member in members)
        {
            _sb.AppendLine(member.Id);
        }

        return _sb.ToString();
    }

    [TransactionalBoundary]
    public async Task RemoveDeactivatedMemberships(IUserStatusChecker userStatusChecker)
    {
        _logger.LogDebug("Started looking for deactivated users");
        if (!await userStatusChecker.TrySetAuthToken())
        {
            _logger.LogError("Unable to Remove Deactivated Memberships, no valid auth token found");
            return;
        }

        var members = await _memberRepository.GetAll();
        List<Member> membersToBeDeleted = new List<Member>();
        List<Member> newlyMissingMembers = new List<Member>();
        List<Member> reappearedMembers = new List<Member>();

        foreach (var member in members)
        {
            // Service principals are not represented in Azure AD's /users endpoint;
            // skipping them keeps the cleaner from incorrectly removing every SP membership.
            if (member.Type == MemberType.ServicePrincipal)
                continue;

            var status = await userStatusChecker.CheckUserStatus(member.Id);

            if (status == UserStatusCheckerStatus.NoAuthToken || status == UserStatusCheckerStatus.BadAuthToken)
            {
                _logger.LogError("Unable to check status of user {UserID}, no valid auth token found", member.Id);
                return;
            }

            if (status == UserStatusCheckerStatus.Found)
            {
                // Only interesting if they were previously marked as missing
                if (await HandleMemberFound(member))
                    reappearedMembers.Add(member);
            }
            else if (status == UserStatusCheckerStatus.NotFound)
            {
                await HandleMemberMissing(
                    member,
                    MissingMemberStatus.NotFound,
                    membersToBeDeleted,
                    newlyMissingMembers
                );
            }
            else if (status == UserStatusCheckerStatus.Deactivated)
            {
                await HandleMemberMissing(
                    member,
                    MissingMemberStatus.Deactivated,
                    membersToBeDeleted,
                    newlyMissingMembers
                );
            }
        }

        LogResults(newlyMissingMembers, reappearedMembers, membersToBeDeleted);

        if (membersToBeDeleted.Count <= 0)
            return;

        await DeleteMembers(membersToBeDeleted);
    }

    private async Task<bool> HandleMemberFound(Member member)
    {
        var missingRecord = await _missingMemberRepository.FindByUser(member.Id.ToString());
        if (missingRecord == null)
            return false;

        await _missingMemberRepository.RemoveByUserId(member.Id.ToString());
        _logger.LogInformation("Member {UserId} reappeared in Azure, removed from missing records", member.Id);
        return true;
    }

    private async Task HandleMemberMissing(
        Member member,
        MissingMemberStatus status,
        List<Member> membersToBeDeleted,
        List<Member> newlyMissingMembers
    )
    {
        var existingRecord = await _missingMemberRepository.FindByUser(member.Id.ToString());

        if (existingRecord == null)
        {
            // First time seeing this member as missing - record it
            var newRecord = new MissingMemberRecord(member.Id.ToString(), status, DateTime.UtcNow);
            await _missingMemberRepository.Add(newRecord);
            newlyMissingMembers.Add(member);
            _logger.LogInformation(
                "Member {UserId} marked as missing with status {Status}. Grace period until {GracePeriodExpiry}",
                member.Id,
                status,
                DateTime.UtcNow.AddDays(GracePeriodDays)
            );
        }
        else
        {
            if (existingRecord.HasGracePeriodExpired(GracePeriodDays))
            {
                membersToBeDeleted.Add(member);
                _logger.LogWarning(
                    "Member {UserId} grace period expired (marked missing since {FirstSeen}). Scheduling for deletion",
                    member.Id,
                    existingRecord.FirstSeenMissingAt
                );
            }
            else
            {
                // Keep heartbeat fresh only while still inside grace period.
                existingRecord.UpdateLastChecked();
                await _missingMemberRepository.Update(existingRecord);

                var daysRemaining =
                    GracePeriodDays - (int)(DateTime.UtcNow - existingRecord.FirstSeenMissingAt).TotalDays;
                _logger.LogInformation(
                    "Member {UserId} still missing with status {Status}. {DaysRemaining} days until deletion",
                    member.Id,
                    status,
                    daysRemaining
                );
            }
        }
    }

    private async Task DeleteMembers(List<Member> membersToBeDeleted)
    {
        foreach (var member in membersToBeDeleted)
        {
            // Clean up RBAC permissions
            await CleanupRbacPermissions(member.Id.ToString());

            // Cancel all memberships
            await _membershipRepository.CancelAllMembershipsWithUserId(member.Id);
        }

        _logger.LogInformation(
            "Successfully cancelled memberships of {Count} users with deactivated/missing accounts",
            membersToBeDeleted.Count
        );

        foreach (var member in membersToBeDeleted)
        {
            await _membershipApplicationRepository.RemoveAllWithUserId(member.Id);
        }

        _logger.LogInformation(
            "Successfully removed pending membership applications of {Count} users",
            membersToBeDeleted.Count
        );

        foreach (var member in membersToBeDeleted)
        {
            // Remove missing-member tracking explicitly to keep behavior consistent
            // even when DB cascades are not enforced (e.g., some test providers).
            await _missingMemberRepository.RemoveByUserId(member.Id.ToString());

            // Remove the member
            await _memberRepository.Remove(member.Id);
        }

        _logger.LogInformation(
            "Successfully removed {Count} member entries of users with deactivated/missing accounts",
            membersToBeDeleted.Count
        );
    }

    private async Task CleanupRbacPermissions(string userId)
    {
        try
        {
            // Remove RBAC permission grants for this user
            var permissionGrants = await _rbacPermissionGrantRepository.GetAllWithPredicate(pg =>
                pg.AssignedEntityId == userId
            );

            foreach (var grant in permissionGrants)
            {
                await _rbacPermissionGrantRepository.Remove(grant.Id);
            }

            if (permissionGrants.Count > 0)
            {
                _logger.LogInformation(
                    "Removed {Count} RBAC permission grants for user {UserId}",
                    permissionGrants.Count,
                    userId
                );
            }

            // Remove RBAC role grants for this user
            var roleGrants = await _rbacRoleGrantRepository.GetByAssignedUsers(new[] { userId });

            foreach (var grant in roleGrants)
            {
                await _rbacRoleGrantRepository.Remove(grant.Id);
            }

            if (roleGrants.Count > 0)
            {
                _logger.LogInformation("Removed {Count} RBAC role grants for user {UserId}", roleGrants.Count, userId);
            }

            // Remove RBAC group memberships for this user
            var groupMembers = await _rbacGroupMemberRepository.GetAllWithPredicate(gm => gm.UserId == userId);

            foreach (var member in groupMembers)
            {
                await _rbacGroupMemberRepository.Remove(member.Id);
            }

            if (groupMembers.Count > 0)
            {
                _logger.LogInformation(
                    "Removed {Count} RBAC group memberships for user {UserId}",
                    groupMembers.Count,
                    userId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up RBAC permissions for user {UserId}", userId);
            throw;
        }
    }

    private void LogResults(
        List<Member> newlyMissingMembers,
        List<Member> reappearedMembers,
        List<Member> membersToBeDeleted
    )
    {
        if (newlyMissingMembers.Count > 0)
        {
            _logger.LogWarning(
                "Found {Count} newly missing members in Azure AD (grace period started):\n{Members}\n",
                newlyMissingMembers.Count,
                ToIdStringList(newlyMissingMembers)
            );
        }
        else
        {
            _logger.LogDebug("No newly missing users found in Azure AD");
        }

        if (reappearedMembers.Count > 0)
        {
            _logger.LogInformation(
                "Found {Count} members that reappeared in Azure AD (cleared from missing records):\n{Members}",
                reappearedMembers.Count,
                ToIdStringList(reappearedMembers)
            );
        }

        if (membersToBeDeleted.Count > 0)
        {
            _logger.LogWarning(
                "Removing {Count} members due to grace period expiration:\n{Members}\n",
                membersToBeDeleted.Count,
                ToIdStringList(membersToBeDeleted)
            );
        }
        else
        {
            _logger.LogDebug("Found no members ready for deletion (grace period not yet expired)");
        }
    }
}
