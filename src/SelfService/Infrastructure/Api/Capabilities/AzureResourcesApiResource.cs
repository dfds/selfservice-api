using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class AzureResourcesApiResource
{
    public AzureResourceApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public AzureResourceListLinks Links { get; set; }

    public class AzureResourceListLinks
    {
        public ResourceLink Self { get; set; }

        public AzureResourceListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public AzureResourcesApiResource(AzureResourceApiResource[] items, AzureResourceListLinks links)
    {
        Items = items;
        Links = links;
    }
}
