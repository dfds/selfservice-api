using System.Text.Json.Serialization;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Infrastructure.Api.Events;

public class EventApiResource
{
    public EventId Id { get; private set; }
    public DateTime EventDate { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public EventType Type { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsUpcoming { get; private set; }
    public EventAttachmentApiResource[] Attachments { get; private set; }

    [JsonPropertyName("_links")]
    public EventLinks Links { get; set; }

    public class EventLinks
    {
        public ResourceLink Self { get; set; }

        public EventLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public EventApiResource(
        EventId id,
        DateTime eventDate,
        string title,
        string description,
        EventType type,
        string createdBy,
        DateTime createdAt,
        bool isUpcoming,
        EventAttachmentApiResource[] attachments,
        EventLinks links
    )
    {
        Id = id;
        EventDate = eventDate;
        Title = title;
        Description = description;
        Type = type;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        IsUpcoming = isUpcoming;
        Attachments = attachments;
        Links = links;
    }
}

public class EventAttachmentApiResource
{
    public EventAttachmentId Id { get; set; }
    public string Url { get; set; }
    public EventAttachmentType Type { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public EventAttachmentApiResource(
        EventAttachmentId id,
        string url,
        EventAttachmentType type,
        string? description,
        DateTime createdAt
    )
    {
        Id = id;
        Url = url;
        Type = type;
        Description = description;
        CreatedAt = createdAt;
    }
}
