using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class DemoRecordingsApiResource
{
    public DemoRecordingApiResource[] Demos { get; set; }

    [JsonPropertyName("_links")]
    public DemoRecordingsLinks Links { get; set; }

    public class DemoRecordingsLinks
    {
        public ResourceLink Self { get; set; }

        public DemoRecordingsLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public DemoRecordingsApiResource(DemoRecordingApiResource[] demos, DemoRecordingsLinks links)
    {
        Demos = demos;
        Links = links;
    }
}
