using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Tests.Builders;

public class CapabilityRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? JsonMetadata { get; set; }
    public List<string>? Invitees { get; set; } = new();

    public CapabilityRequest(string? name, string? description, string? jsonMetadata, List<string>? invitees)
    {
        Name = name;
        Description = description;
        JsonMetadata = jsonMetadata;
        Invitees = invitees;
    }
}

public class CapabilityRequestBuilder
{
    private string _name;
    private string _description;
    private string _jsonMetadata;
    private List<string> _invitees;

    public CapabilityRequestBuilder()
    {
        _name = "foo-bar";
        _description = "this is foo and bar";
        _jsonMetadata = "{}";
        _invitees = new List<string>();
    }

    public CapabilityRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CapabilityRequestBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CapabilityRequestBuilder WithJsonMetadata(string jsonMetadata)
    {
        _jsonMetadata = jsonMetadata;
        return this;
    }

    public CapabilityRequestBuilder WithInvitees(List<string> invitees)
    {
        _invitees = invitees;
        return this;
    }

    public CapabilityRequest Build()
    {
        var c = new CapabilityRequest(
            name: _name,
            description: _description,
            jsonMetadata: _jsonMetadata,
            invitees: _invitees
        );
        return c;
    }

    public static implicit operator CapabilityRequest(CapabilityRequestBuilder builder) => builder.Build();
}
