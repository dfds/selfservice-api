using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityDetailsApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityDetailsLinks Links { get; set; } = new();

    public class CapabilityDetailsLinks
    {
        public ResourceLink Self { get; set; } = new();
        public ResourceLink Members { get; set; } = new();
        public ResourceLink Clusters { get; set; } = new();
        public ResourceLink MembershipApplications { get; set; } = new();
        public ResourceLink LeaveCapability { get; set; } = new();
        public ResourceLink AwsAccount { get; set; } = new();
    }
}

public class CapabilityListItemApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityListItemLinks Links { get; set; } = new();

    public class CapabilityListItemLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}