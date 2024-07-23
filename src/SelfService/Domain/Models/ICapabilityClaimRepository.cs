using System.Security.Claims;

namespace SelfService.Domain.Models;

public interface ICapabilityClaimRepository
{
    Task Add(CapabilityClaim claim);

    Task<bool> CheckClaim(CapabilityId capabilityId, string claimType); // TODO: enum

    Task<List<CapabilityClaim>> GetAll(CapabilityId capabilityId);
}
