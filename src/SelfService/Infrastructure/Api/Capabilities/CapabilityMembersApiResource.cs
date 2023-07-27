using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityMembersApiResource
{
    public MemberApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityMembersLinks Links { get; set; } = new();

    public class CapabilityMembersLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}