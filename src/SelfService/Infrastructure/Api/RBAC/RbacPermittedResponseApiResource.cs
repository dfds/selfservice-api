using System.Text.Json.Serialization;
using SelfService.Application;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.RBAC;

public class RbacPermittedResponseApiResource : PermittedResponse
{
    [JsonPropertyName("_links")]
    public required RLinks Links { get; set; }
    
    public class RLinks
    {
        public ResourceLink? CanI { get; set; }
        public ResourceLink? CanThey { get; set; }
    }
}