using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Events;

public class EventsApiResource
{
    public EventApiResource[] Events { get; set; }

    [JsonPropertyName("_links")]
    public EventsLinks Links { get; set; }

    public class EventsLinks
    {
        public ResourceLink Self { get; set; }

        public EventsLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public EventsApiResource(EventApiResource[] events, EventsLinks links)
    {
        Events = events;
        Links = links;
    }
}
