namespace SelfService.Domain.Models;

public enum EventAttachmentType
{
    Document,
    Recording,
    Image,
    Other
}

public class EventAttachment : Entity<EventAttachmentId>
{
    public EventId EventId { get; private set; }
    public string Url { get; private set; }
    public EventAttachmentType AttachmentType { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public EventAttachment(
        EventAttachmentId id,
        EventId eventId,
        string url,
        EventAttachmentType attachmentType,
        string? description,
        DateTime createdAt
    )
        : base(id)
    {
        EventId = eventId;
        Url = url;
        AttachmentType = attachmentType;
        Description = description;
        CreatedAt = createdAt;
    }

    public void Update(string? url, EventAttachmentType? attachmentType, string? description)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            Url = url;
        }

        if (attachmentType.HasValue)
        {
            AttachmentType = attachmentType.Value;
        }

        if (description != null)
        {
            Description = description;
        }
    }
}
