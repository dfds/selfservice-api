using System.Text;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Application;

public class DeactivatedMemberCleanerApplicationService
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;

    private readonly ILogger<DeactivatedMemberCleanerApplicationService> _logger;

    public DeactivatedMemberCleanerApplicationService(
        ILogger<DeactivatedMemberCleanerApplicationService> logger,
        IMembershipRepository membershipRepository,
        IMemberRepository memberRepository,
        IMembershipApplicationRepository membershipApplicationRepository
    )
    {
        _membershipRepository = membershipRepository;
        _memberRepository = memberRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _logger = logger;
    }

    public async Task RemoveDeactivatedMemberships(IUserStatusChecker userStatusChecker)
    {
        _logger.LogDebug("Started looking for deactivated users");
        var members = await _memberRepository.GetAll();
        List<Member> deactivatedMembers = new List<Member>();
        List<Member> notFoundMembers = new List<Member>();
        StringBuilder deactivatedMembersStringBuilder = new StringBuilder();
        StringBuilder notFoundMembersStringBuilder = new StringBuilder();
        foreach (var member in members)
        {
            var (isDeactivated, reason) = await userStatusChecker.CheckUserStatus(member.Id);
            if (isDeactivated)
            {
                if (reason == "NotFound")
                {
                    notFoundMembers.Add(member);
                    notFoundMembersStringBuilder.AppendLine(member.Id);
                }
                if (reason == "Deactivated")
                {
                    deactivatedMembers.Add(member);
                    deactivatedMembersStringBuilder.AppendLine(member.Id);
                }
            }
        }
        if (notFoundMembers.Count <= 0)
        {
            _logger.LogDebug("no users were completely unfound in Azure AD (yay)");
        }
        if (deactivatedMembers.Count <= 0)
        {
            _logger.LogDebug("Found no members with deactivated/disabled accounts");
        }

        _logger.LogWarning(
            "[TRIAL] following {NotFoundmembersCount} members not found in Azure AD:\n{notfoundMembers}\n NOT deleting for now",
            notFoundMembers.Count,
            notFoundMembersStringBuilder.ToString()
        );

        _logger.LogDebug(
            "Removing {DeactivatedMembersCount} members, disabled or not found in Azure AD:\\n{DeactivatedMembers}",
            deactivatedMembers.Count,
            deactivatedMembersStringBuilder.ToString()
        );

        List<Member> membersToBeDeleted = deactivatedMembers;

        // membersToBeDeleted = deactivatedMembers
        //     .Concat(notFoundMembers)
        //     .ToHashSet().ToList(); //removal of duplicates if an Id is in both lists, though this shouldn't happen

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
    }
}
