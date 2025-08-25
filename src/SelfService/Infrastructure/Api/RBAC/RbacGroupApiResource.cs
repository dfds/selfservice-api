using System.Text.Json.Serialization;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.RBAC;

public class RbacGroupApiResource
{
    public string Id { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ICollection<RbacGroupMember> Members { get; set; } = new List<RbacGroupMember>();

    [JsonPropertyName("_links")]
    public required RLinks Links { get; set; }

    public class RLinks
    {
        public ResourceLink Self { get; set; }

        public RLinks(ResourceLink self)
        {
            Self = self;
        }
    }
}
