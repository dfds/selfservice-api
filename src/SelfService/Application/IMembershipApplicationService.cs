using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IMembershipApplicationService
{
    Task<MembershipId> StartNewMembership(MembershipApplicationId applicationId);
    Task<MembershipApplicationId> SubmitMembershipApplication(CapabilityId capabilityId, UserId userId);
}