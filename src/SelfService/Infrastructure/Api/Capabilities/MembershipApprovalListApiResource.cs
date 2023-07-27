using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApprovalListApiResource
{
    public MembershipApprovalApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public MembershipApprovalListLinks Links { get; set; } = new();

    public class MembershipApprovalListLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}