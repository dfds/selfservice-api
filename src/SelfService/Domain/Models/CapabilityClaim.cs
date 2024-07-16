namespace SelfService.Domain.Models;

public class CapabilityClaim : AggregateRoot<CapabilityClaimId>
{
    public CapabilityClaim(
        CapabilityClaimId id,
        string claim,
        CapabilityId capabilityId,
        DateTime requestedAt,
        string requestedBy
    )
        : base(id)
    {
        CapabilityId = capabilityId;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
        Claim = claim;
    }
    public CapabilityId CapabilityId { get; private set; }
    public string Claim { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }
    
   
}