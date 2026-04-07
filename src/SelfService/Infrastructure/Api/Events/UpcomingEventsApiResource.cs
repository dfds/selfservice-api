using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Events;

public class UpcomingEventsApiResource
{
    public EventApiResource[] UpcomingEvents { get; set; }
    public EventApiResource? LatestHeldEvent { get; set; }

    [JsonPropertyName("_links")]
    public UpcomingEventsLinks Links { get; set; }

    public class UpcomingEventsLinks
    {
        public ResourceLink Self { get; set; }

        public UpcomingEventsLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public UpcomingEventsApiResource(
        EventApiResource[] upcomingEvents,
        EventApiResource? latestHeldEvent,
        UpcomingEventsLinks links
    )
    {
        UpcomingEvents = upcomingEvents;
        LatestHeldEvent = latestHeldEvent;
        Links = links;
    }
}
