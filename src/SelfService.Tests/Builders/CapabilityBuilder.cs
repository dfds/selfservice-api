using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class CapabilityBuilder
{
    private CapabilityId _id;
    private string _name;
    private string _description;
    private DateTime? _deleted;
    private DateTime _createdAt;
    private string _createdBy;

    public CapabilityBuilder()
    {
        _id = CapabilityId.CreateFrom("foo");
        _name = "foo";
        _description = "this is foo";
        _deleted = null;
        _createdAt = new DateTime(2000, 1, 1);
        _createdBy = nameof(CapabilityBuilder);
    }

    public CapabilityBuilder WithId(CapabilityId id)
    {
        _id = id;
        return this;
    }

    public CapabilityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public Capability Build()
    {
        return new Capability(
            id: _id,
            name: _name,
            description: _description,
            deleted: _deleted,
            createdAt: _createdAt,
            createdBy: _createdBy
        );
    }

    public static implicit operator Capability(CapabilityBuilder builder)
        => builder.Build();
}