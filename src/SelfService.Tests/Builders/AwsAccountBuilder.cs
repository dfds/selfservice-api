using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class AwsAccountBuilder
{
    private AwsAccountId _id;
    private CapabilityId _capabilityId;
    private string _environment;
    private DateTime _createdAt;
    private string _createdBy;

    public AwsAccountBuilder()
    {
        _id = AwsAccountId.New();
        _capabilityId = CapabilityId.Parse("foo");
        _environment = "prod";
        _createdAt = new DateTime(2000, 1, 1);
        _createdBy = nameof(AwsAccountBuilder);
    }

    public AwsAccountBuilder WithCapabilityId(CapabilityId capabilityId)
    {
        _capabilityId = capabilityId;
        return this;
    }

    public AwsAccount Build()
    {
        return new AwsAccount(
            id: _id,
            capabilityId: _capabilityId,
            environment: _environment,
            requestedAt: _createdAt,
            requestedBy: _createdBy
        );
    }

    public static implicit operator AwsAccount(AwsAccountBuilder builder) => builder.Build();
}
