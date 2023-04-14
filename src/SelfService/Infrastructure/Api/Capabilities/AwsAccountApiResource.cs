using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class AwsAccountApiResource
{
    public string Id { get; set; }
    public string? AwsAccountId { get; set; }
    public string? RoleEmail { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public string? KubernetesNamespace { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Status { get; set; }

    [JsonPropertyName("_links")]
    public AwsAccountLinks Links { get; set; } = new();

    public class AwsAccountLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}