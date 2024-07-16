namespace SelfService.Domain.Models;

public interface ICapabilityClaimService
{
    Task<CapabilityClaimId> Add(CapabilityId capabilityId, string requestClaim, UserId userId);
}