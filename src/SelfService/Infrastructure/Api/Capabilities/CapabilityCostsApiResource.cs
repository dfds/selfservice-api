using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityCostsApiResource
{

    [JsonPropertyName("_links")]
    public CapabilityCostsLinks Links { get; set; }

    public class CapabilityCostsLinks
    {
        public ResourceLink Self { get; set; }

        public CapabilityCostsLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public CapabilityCostsApiResource(CapabilityCostsLinks links)
    {
        Links = links;
    }
}
