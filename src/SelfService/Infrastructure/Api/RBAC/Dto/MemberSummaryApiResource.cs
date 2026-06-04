using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.RBAC.Dto;

public class MemberSummaryApiResource
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Type { get; set; } = "";
    public DateTime? LastSeen { get; set; }

    [JsonPropertyName("_links")]
    public MemberSummaryLinks? Links { get; set; }

    public class MemberSummaryLinks
    {
        public ResourceLink? Self { get; set; }
        public ResourceLink? PermissionGrants { get; set; }
        public ResourceLink? RoleGrants { get; set; }
        public ResourceLink? Groups { get; set; }
    }
}

public class MemberSummaryListApiResource
{
    public List<MemberSummaryApiResource> Items { get; set; } = new();
    public int Total { get; set; }
}
