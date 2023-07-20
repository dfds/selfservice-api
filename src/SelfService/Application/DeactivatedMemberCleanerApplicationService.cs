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
        IMembershipApplicationRepository membershipApplicationRepository)
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
        StringBuilder sb0 = new StringBuilder();
        StringBuilder sb1 = new StringBuilder();
        foreach (var member in members)
        {
            var (isDeactivated,reason) = await userStatusChecker.MakeUserRequest(member.Id);
            if(isDeactivated){
                if (reason == "NotFound")
                {
                    notFoundMembers.Add(member);
                    sb1.AppendLine(member.Id);
                }else{
                    deactivatedMembers.Add(member);
                    sb0.AppendLine(member.Id);
                }
            }
        }
        if (notFoundMembers.Count <= 0)
        {
            _logger.LogDebug("no users were completely unfound in Azure AD (yay)");
            return;
        }
        if (deactivatedMembers.Count <= 0)
        {
            _logger.LogDebug("Found no deactivated users");
            return;
        }

        _logger.LogDebug(
            "[TRIAL] following {NotFoundmembersCount} users not found in Azure AD:\\n{notfoundUsers}\\n not deleting for now",
            notFoundMembers.Count, sb1.ToString());

        _logger.LogDebug(
            "Removing {DeactivatedMembersCount} users, disabled or not found in Azure AD:\\n{DeactivatedUsers}",
            deactivatedMembers.Count, sb0.ToString());


        foreach (var deactivatedMember in deactivatedMembers)
        {
            await _membershipRepository.CancelAllMembershipsWithUserId(deactivatedMember.Id);
        }

        _logger.LogDebug("Successfully cancelled memberships of users with deactivated accounts");

        foreach (var deactivatedMember in deactivatedMembers)
        {
            await _membershipApplicationRepository.RemoveAllWithUserId(deactivatedMember.Id);
        }

        _logger.LogDebug("Successfully removed pending applications of users with deactivated accounts");

        foreach (var deactivatedMember in deactivatedMembers)
        {
            await _memberRepository.Remove(deactivatedMember.Id);
        }

        _logger.LogDebug("Successfully removed deactivated members with deactivated accounts");
    }
}