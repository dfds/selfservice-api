using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.RBAC;

public class RbacPermissionGrantApiResource
{
    public string Id { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string AssignedEntityType { get; set; } = "";
    public string AssignedEntityId { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Permission { get; set; } = "";
    public string Type { get; set; } = "";
    public string Resource { get; set; } = "";

    [JsonPropertyName("_links")]
    public required RbacPermissionGrantLinks Links { get; set; }

    public class RbacPermissionGrantLinks
    {
        public ResourceLink? Self { get; set; }
        public ResourceLink? GrantPermission { get; set; }
        public ResourceLink? RevokePermission { get; set; }
    }
}
