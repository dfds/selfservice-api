using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Teams;

public class TeamApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public string CreatedBy { get; set; }

    public string CreatedAt { get; set; }

    [JsonPropertyName("_links")]
    public TeamLinks Links { get; set; }

    public class TeamLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink Capabilities { get; set; }
        public ResourceLink Members { get; set; }

        public TeamLinks(ResourceLink self, ResourceLink capabilities, ResourceLink members)
        {
            Self = self;
            Capabilities = capabilities;
            Members = members;
        }
    }

    public TeamApiResource(
        string id,
        string name,
        string description,
        string createdBy,
        string createdAt,
        TeamLinks links
    )
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        Links = links;
    }
}
