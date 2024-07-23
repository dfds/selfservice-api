using System.Security.Claims;

namespace SelfService.Domain.Models;

public interface ICapabilityClaimRepository
{
    Task Add(CapabilityClaim claim);

    Task<bool> ClaimExists(CapabilityId capabilityId, string claimType);

    Task<List<CapabilityClaim>> GetAll(CapabilityId capabilityId);
}
