using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityMembersApiResource
{
    public MemberApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityMembersLinks Links { get; set; }

    public class CapabilityMembersLinks
    {
        public ResourceLink Self { get; set; }

        public CapabilityMembersLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public CapabilityMembersApiResource(MemberApiResource[] items, CapabilityMembersLinks links)
    {
        Items = items;
        Links = links;
    }
}
