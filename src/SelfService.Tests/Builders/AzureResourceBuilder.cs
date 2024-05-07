using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class AzureResourceBuilder
{
    private AzureResourceId _id;
    private string _environment;
    private CapabilityId _capabilityId;
    private DateTime _createdAt;
    private string _createdBy;

    public AzureResourceBuilder()
    {
        _id = AzureResourceId.New();
        _environment = "test";
        _capabilityId = CapabilityId.Parse("bar");
        _createdAt = new DateTime(2024, 1, 1);
        _createdBy = nameof(AzureResourceBuilder);
    }

    public AzureResourceBuilder WithCapabilityId(CapabilityId capabilityId)
    {
        _capabilityId = capabilityId;
        return this;
    }

    public AzureResource Build()
    {
        return new AzureResource(
            id: _id,
            environment: _environment,
            capabilityId: _capabilityId,
            requestedAt: _createdAt,
            requestedBy: _createdBy
        );
    }

    public static implicit operator AzureResource(AzureResourceBuilder builder) => builder.Build();
}
