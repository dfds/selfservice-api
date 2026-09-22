using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KubernetesAccessApiResource
{
    public string Id { get; set; }
    public string CapabilityId { get; set; }
    public string Environment { get; set; }
    public string? AwsAccountId { get; set; }
    public string? Namespace { get; set; }
    public string? Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public string RequestedBy { get; set; }

    [JsonPropertyName("_links")]
    public KubernetesAccessLinks Links { get; set; }

    public class KubernetesAccessLinks
    {
        public ResourceLink Self { get; set; }

        public KubernetesAccessLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public KubernetesAccessApiResource(
        string id,
        string capabilityId,
        string environment,
        string? awsAccountId,
        string? @namespace,
        string? status,
        DateTime requestedAt,
        string requestedBy,
        KubernetesAccessLinks links
    )
    {
        Id = id;
        CapabilityId = capabilityId;
        Environment = environment;
        AwsAccountId = awsAccountId;
        Namespace = @namespace;
        Status = status;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
        Links = links;
    }
}
