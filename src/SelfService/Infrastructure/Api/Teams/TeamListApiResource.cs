using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Teams;

public class TeamListApiResource
{
    public TeamApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public TeamListLinks Links { get; set; }

    public class TeamListLinks
    {
        public ResourceLink Self { get; set; }

        public TeamListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public TeamListApiResource(TeamApiResource[] items, TeamListLinks links)
    {
        Items = items;
        Links = links;
    }
}
