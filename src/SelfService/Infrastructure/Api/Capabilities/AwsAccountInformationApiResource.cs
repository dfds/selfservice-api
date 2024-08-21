using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class AwsAccountInformationApiResource
{
    public string Id { get; set; }
    public string? CapabilityId { get; set; }
    public List<VPCInformation>? vpcs { get; set; }

    [JsonPropertyName("_links")]
    public AwsAccountInformationLinks Links { get; set; }

    public class AwsAccountInformationLinks
    {
        public ResourceLink Self { get; set; }

        public AwsAccountInformationLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public AwsAccountInformationApiResource(
        string id,
        string? capabilityId,
        List<VPCInformation>? vpcs,
        AwsAccountInformationLinks links
    )
    {
        Id = id;
        CapabilityId = capabilityId;
        this.vpcs = vpcs;
        Links = links;
    }
}
