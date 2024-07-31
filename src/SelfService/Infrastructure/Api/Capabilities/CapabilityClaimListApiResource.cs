using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityClaimListApiResource
{
    public List<CapabilityClaimApiResource> Claims { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityClaimListLinks Links { get; set; }

    public class CapabilityClaimListLinks
    {
        public ResourceLink Self { get; set; }

        public CapabilityClaimListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public CapabilityClaimListApiResource(List<CapabilityClaimApiResource> claims, CapabilityClaimListLinks links)
    {
        Claims = claims;
        Links = links;
    }
}
