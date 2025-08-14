using System.Text.Json.Serialization;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.RBAC;

public class RbacPermittedResponseApiResource
{
    public Dictionary<String, PermissionMatrix> PermissionMatrix { get; set; } = new();
    public List<RbacPermissionGrant> PermissionGrants { get; set; } = new();
    [JsonPropertyName("_links")]
    public required RbacPermittedResponseLinks Links { get; set; }
    
    public class RbacPermittedResponseLinks
    {
        public ResourceLink? CanI { get; set; }
        public ResourceLink? CanThey { get; set; }
    }
}