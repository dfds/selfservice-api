using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class AzureResourceApiResource
{
    public string Id { get; set; }

    public string Environment {  get; set; }

    [JsonPropertyName("_links")]
    public AzureResourceLinks Links { get; set; }

    public class AzureResourceLinks
    {
        public ResourceLink Self { get; set; }

        public AzureResourceLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public AzureResourceApiResource(string id, string environment, AzureResourceLinks links)
    {
        Id = id;
        Environment = environment;
        Links = links;
    }
}
