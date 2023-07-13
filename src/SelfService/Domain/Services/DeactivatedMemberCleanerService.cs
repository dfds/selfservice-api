using System.Text;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Domain.Services;

public class DeactivatedMemberCleanerService
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;

    private readonly ILogger<DeactivatedMemberCleanerService> _logger;

    public DeactivatedMemberCleanerService(
        ILogger<DeactivatedMemberCleanerService> logger,
        IMembershipRepository membershipRepository,
        IMemberRepository memberRepository,
        IMembershipApplicationRepository membershipApplicationRepository)
    {
        _membershipRepository = membershipRepository;
        _memberRepository = memberRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _logger = logger;
    }

    public async Task RemoveDeactivatedMemberships(UserStatusChecker userStatusChecker)
    {
        _logger.LogDebug("Started looking for deactivated users");
        var members = await _memberRepository.GetAll();
        List<Member> deactivatedMembers = new List<Member>();
        StringBuilder sb = new StringBuilder();
        foreach (var member in members)
        {
            var (isDeactivated,reason) = await userStatusChecker.MakeUserRequest(member.Id);
            if(isDeactivated)
                continue;
            // TODO: print something nice with reason in case of weird state
            deactivatedMembers.Add(member);
            sb.AppendLine(member.Id);
        }

        if (deactivatedMembers.Count <= 0)
        {
            _logger.LogDebug("Found no deactivated users");
            return;
        }

        _logger.LogDebug(
            "Removing {DeactivatedMembersCount} users, disabled or not found in Azure AD:\\n{DeactivatedUsers}",
            deactivatedMembers.Count, sb.ToString());


        foreach (var deactivatedMember in deactivatedMembers)
        {
            await _membershipRepository.CancelAllMembershipsWithUserId(deactivatedMember.Id);
        }

        _logger.LogDebug("Successfully cancelled memberships of users");

        foreach (var deactivatedMember in deactivatedMembers)
        {
            await _membershipApplicationRepository.RemoveAllWithUserId(deactivatedMember.Id);
        }

        _logger.LogDebug("Successfully removed pending applications for users");

        foreach (var deactivatedMember in deactivatedMembers)
        {
            await _memberRepository.Remove(deactivatedMember.Id);
        }

        _logger.LogDebug("Successfully removed users");
    }
    
}