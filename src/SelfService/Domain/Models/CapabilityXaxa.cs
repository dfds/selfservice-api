namespace SelfService.Domain.Models;

public class CapabilityXaxa : AggregateRoot<CapabilityXaxaId>
{
    public CapabilityXaxa(
        CapabilityXaxaId id,
        string environment,
        CapabilityId capabilityId,
        DateTime requestedAt,
        string requestedBy
    )
        : base(id)
    {
        CapabilityId = capabilityId;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
        Environment = environment;
    }

    public CapabilityId CapabilityId { get; private set; }
    public string Environment { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }

    public static CapabilityXaxa RequestNew(
        CapabilityId capabilityId,
        string environment,
        DateTime requestedAt,
        string requestedBy
    )
    {
        var resource = new CapabilityXaxa(
            id: CapabilityXaxaId.New(),
            capabilityId: capabilityId,
            environment: environment,
            requestedAt: requestedAt,
            requestedBy: requestedBy
        );
        
        return resource;
    }
}
