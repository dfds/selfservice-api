using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class DemosApiResource
{
    public DemoApiResource[] Demos { get; set; }

    [JsonPropertyName("_links")]
    public DemosLinks Links { get; set; }

    public class DemosLinks
    {
        public ResourceLink Self { get; set; }

        public DemosLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public DemosApiResource(DemoApiResource[] demos, DemosLinks links)
    {
        Demos = demos;
        Links = links;
    }
}
