using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class CapabilityBuilder
{
    private CapabilityId _id;
    private string _name;
    private string _description;
    private CapabilityStatusOptions _status;
    private DateTime _createdAt;
    private DateTime _modifiedAt;
    private string _createdBy;
    private string _jsonMetadata;

    public CapabilityBuilder()
    {
        _id = CapabilityId.CreateFrom("foo");
        _name = "foo";
        _description = "this is foo";
        _status = CapabilityStatusOptions.Active;
        _createdAt = DateTime.Now;
        _modifiedAt = DateTime.Now;
        _createdBy = nameof(CapabilityBuilder);
        _jsonMetadata = "";
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

    public CapabilityBuilder WithModifiedAt(DateTime modifiedAt)
    {
        _modifiedAt = modifiedAt;
        return this;
    }

    public CapabilityBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CapabilityBuilder WithStatus(CapabilityStatusOptions status)
    {
        _status = status;
        return this;
    }

    public CapabilityBuilder WithJsonMetadata(string jsonMetadata)
    {
        _jsonMetadata = jsonMetadata;
        return this;
    }

    public Capability Build()
    {
        var c = new Capability(
            id: _id,
            name: _name,
            description: _description,
            createdAt: _createdAt,
            createdBy: _createdBy,
            jsonMetadata: _jsonMetadata
        );
        c.Status = _status;
        c.SetModifiedDate(_modifiedAt);

        return c;
    }

    public static implicit operator Capability(CapabilityBuilder builder) => builder.Build();
}
