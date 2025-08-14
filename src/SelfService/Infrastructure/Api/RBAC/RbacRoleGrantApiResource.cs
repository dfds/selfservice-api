using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.RBAC;

public class RbacRoleGrantApiResource
{
    public string Id { get; set; } = "";
    public string RoleId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string AssignedEntityType { get; set; } = "";
    public string AssignedEntityId { get; set; } = "";
    public string Type { get; set; } = "";
    public string Resource { get; set; } = "";
    
    [JsonPropertyName("_links")]
    public required RbacRoleGrantLinks Links { get; set; }
    
    public class RbacRoleGrantLinks
    {
        public ResourceLink? Self { get; set; }
        public ResourceLink? GrantRole { get; set; }
        public ResourceLink? RevokeRole { get; set; }
    }
}