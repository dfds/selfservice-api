using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class DemoRecordingApiResource
{
    public DemoRecordingId Id { get; private set; }
    public DateTime RecordingDate { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Url { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    [JsonPropertyName("_links")]
    public DemoRecordingLinks Links { get; set; }

    public class DemoRecordingLinks
    {
        public ResourceLink Self { get; set; }

        public DemoRecordingLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public DemoRecordingApiResource(
        DemoRecordingId id,
        DateTime recordingDate,
        string title,
        string description,
        string url,
        string createdBy,
        DateTime createdAt,
        DemoRecordingLinks links
    )
    {
        Id = id;
        RecordingDate = recordingDate;
        Title = title;
        Description = description;
        Url = url;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        Links = links;
    }
}
