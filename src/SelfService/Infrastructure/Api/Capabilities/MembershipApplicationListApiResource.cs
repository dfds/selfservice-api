using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApplicationListApiResource
{
    public MembershipApplicationApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public MembershipApplicationListLinks Links { get; set; } = new();

    public class MembershipApplicationListLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}