using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IMembershipApplicationService
{
    Task<MembershipId> AcceptApplication(MembershipApplicationId applicationId);
    Task<MembershipApplicationId> SubmitMembershipApplication(CapabilityId capabilityId, UserId userId);
    Task CancelExpiredMembershipApplications();
    Task TryFinalizeMembershipApplication(MembershipApplicationId applicationId);
    Task ApproveMembershipApplication(MembershipApplicationId applicationId, UserId approvedBy);
    Task AddCreatorAsInitialMember(CapabilityId capabilityId, UserId creatorId);
}