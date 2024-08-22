using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityClaimApiResource
{
    public string Claim { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public string ClaimDescription { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityClaimLinks Links { get; set; }

    public class CapabilityClaimLinks
    {
        public ResourceLink? Claim { get; set; }

        public CapabilityClaimLinks(ResourceLink? claim)
        {
            Claim = claim;
        }
    }

    public CapabilityClaimApiResource(
        string claim,
        string claimDescription,
        DateTime? claimedAt,
        CapabilityClaimLinks links
    )
    {
        Claim = claim;
        ClaimDescription = claimDescription;
        ClaimedAt = claimedAt;
        Links = links;
    }
}
