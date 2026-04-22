using System.Text.Json.Serialization;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.News;

public class NewsItemApiResource
{
    public NewsItemId Id { get; private set; }
    public string Title { get; private set; }
    public string Body { get; private set; }
    public DateTime DueDate { get; private set; }
    public bool IsHighlighted { get; private set; }
    public bool IsRelevant { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    [JsonPropertyName("_links")]
    public NewsItemLinks Links { get; set; }

    public class NewsItemLinks
    {
        public ResourceLink Self { get; set; }

        public NewsItemLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public NewsItemApiResource(
        NewsItemId id,
        string title,
        string body,
        DateTime dueDate,
        bool isHighlighted,
        bool isRelevant,
        string createdBy,
        DateTime createdAt,
        DateTime? modifiedAt,
        NewsItemLinks links
    )
    {
        Id = id;
        Title = title;
        Body = body;
        DueDate = dueDate;
        IsHighlighted = isHighlighted;
        IsRelevant = isRelevant;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
        Links = links;
    }
}
