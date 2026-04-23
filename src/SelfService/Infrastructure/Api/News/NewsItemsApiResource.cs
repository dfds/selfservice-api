using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.News;

public class NewsItemsApiResource
{
    public NewsItemApiResource[] NewsItems { get; set; }

    [JsonPropertyName("_links")]
    public NewsItemsLinks Links { get; set; }

    public class NewsItemsLinks
    {
        public ResourceLink Self { get; set; }

        public NewsItemsLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public NewsItemsApiResource(NewsItemApiResource[] newsItems, NewsItemsLinks links)
    {
        NewsItems = newsItems;
        Links = links;
    }
}
