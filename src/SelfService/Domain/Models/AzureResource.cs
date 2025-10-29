using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class AzureResource : AggregateRoot<AzureResourceId>
{
    public AzureResource(
        AzureResourceId id,
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

    public static AzureResource RequestNew(
        CapabilityId capabilityId,
        string environment,
        DateTime requestedAt,
        string requestedBy,
        string purpose,
        string? catalogueId = null,
        string? risk = null,
        bool? gdpr = null
    )
    {
        var resource = new AzureResource(
            id: AzureResourceId.New(),
            capabilityId: capabilityId,
            environment: environment,
            requestedAt: requestedAt,
            requestedBy: requestedBy
        );

        resource.Raise(
            new AzureResourceRequested()
            {
                AzureResourceId = resource.Id,
                Purpose = purpose,
                CatalogueId = catalogueId,
                Risk = risk,
                Gdpr = gdpr,
                Owner = requestedBy
            }
        );

        return resource;
    }
}
