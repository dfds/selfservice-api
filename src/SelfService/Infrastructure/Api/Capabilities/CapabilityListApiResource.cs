using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityListApiResource
{
    public CapabilityListItemApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityListLinks Links { get; set; }

    public class CapabilityListLinks
    {
        public ResourceLink Self { get; set; }

        public CapabilityListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public CapabilityListApiResource(CapabilityListItemApiResource[] items, CapabilityListLinks links)
    {
        Items = items;
        Links = links;
    }
}
