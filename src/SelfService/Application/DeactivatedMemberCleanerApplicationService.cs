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

    private readonly ILogger<DeactivatedMemberCleanerApplicationService> _logger;

    private readonly IInvitationRepository _invitationRepository;

    private readonly StringBuilder _sb = new();

    public DeactivatedMemberCleanerApplicationService(
        ILogger<DeactivatedMemberCleanerApplicationService> logger,
        IMembershipRepository membershipRepository,
        IMemberRepository memberRepository,
        IMembershipApplicationRepository membershipApplicationRepository,
        IInvitationRepository invitationRepository
    )
    {
        _membershipRepository = membershipRepository;
        _memberRepository = memberRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _logger = logger;
        _invitationRepository = invitationRepository;
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
        List<Member> deactivatedMembers = new List<Member>();
        List<Member> notFoundMembers = new List<Member>();
        foreach (var member in members)
        {
            var status = await userStatusChecker.CheckUserStatus(member.Id);

            if (status == UserStatusCheckerStatus.NotFound)
                notFoundMembers.Add(member);
            if (status == UserStatusCheckerStatus.Deactivated)
                deactivatedMembers.Add(member);

            if (status == UserStatusCheckerStatus.NoAuthToken || status == UserStatusCheckerStatus.BadAuthToken)
            {
                _logger.LogError("Unable to check status of user {UserID}, no valid auth token found", member.Id);
                return;
            }
        }

        if (notFoundMembers.Count <= 0)
        {
            _logger.LogDebug("no users were completely unfound in Azure AD (yay)");
        }
        else
        {
            _logger.LogWarning(
                "Removing {NotFoundmembersCount} members not found in Azure AD:\n{notfoundMembers}\n",
                notFoundMembers.Count,
                ToIdStringList(notFoundMembers)
            );
        }

        if (deactivatedMembers.Count <= 0)
        {
            _logger.LogDebug("Found no members with deactivated/disabled accounts");
        }
        else
        {
            _logger.LogDebug(
                "Removing {DeactivatedMembersCount} members, disabled or not found in Azure AD:\\n{DeactivatedMembers}",
                deactivatedMembers.Count,
                ToIdStringList(deactivatedMembers)
            );
        }

        var membersToBeDeleted = new HashSet<Member>();
        deactivatedMembers.ForEach(x => membersToBeDeleted.Add(x));
        notFoundMembers.ForEach(x => membersToBeDeleted.Add(x));

        if (membersToBeDeleted.Count <= 0)
            return;

        foreach (var member in membersToBeDeleted)
        {
            await _membershipRepository.CancelAllMembershipsWithUserId(member.Id);
        }

        _logger.LogDebug("Successfully cancelled memberships of users with deactivated accounts");

        foreach (var member in membersToBeDeleted)
        {
            await _membershipApplicationRepository.RemoveAllWithUserId(member.Id);
        }

        _logger.LogDebug("Successfully removed pending membership applications of users with deactivated accounts");

        foreach (var member in membersToBeDeleted)
        {
            await _memberRepository.Remove(member.Id);
        }

        _logger.LogDebug("Successfully removed member entries of users with deactivated accounts");

        foreach (var member in membersToBeDeleted)
        {
            var invitations = await _invitationRepository.GetAllWithPredicate(x => x.Invitee == member.Id);
            foreach (var invitation in invitations)
            {
                await _invitationRepository.Remove(invitation.Id);
            }
        }

        _logger.LogDebug("Successfully removed invitations for users with deactivated accounts");
    }
}
