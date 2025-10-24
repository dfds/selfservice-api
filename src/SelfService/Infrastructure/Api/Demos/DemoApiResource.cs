using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class DemoApiResource
{
    public DemoId Id { get; private set; }
    public DateTime RecordingDate { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Uri { get; private set; }
    public string Tags { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    [JsonPropertyName("_links")]
    public DemoLinks Links { get; set; }

    public class DemoLinks
    {
        public ResourceLink Self { get; set; }

        public DemoLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public DemoApiResource(
        DemoId id,
        DateTime recordingDate,
        string title,
        string description,
        string uri,
        string tags,
        string createdBy,
        DateTime createdAt,
        bool isActive,
        DemoLinks links
    )
    {
        Id = id;
        RecordingDate = recordingDate;
        Title = title;
        Description = description;
        Uri = uri;
        Tags = tags;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        IsActive = isActive;
        Links = links;
    }
}
