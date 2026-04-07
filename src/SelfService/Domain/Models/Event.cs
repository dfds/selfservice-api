namespace SelfService.Domain.Models;

public enum EventType
{
    Demo,
    Workshop,
    Informational,
    Other
}

public class Event : Entity<EventId>
{
    public DateTime EventDate { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public EventType Type { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<EventAttachment> _attachments = new();
    public IReadOnlyCollection<EventAttachment> Attachments => _attachments.AsReadOnly();

    public Event(
        EventId id,
        DateTime eventDate,
        string title,
        string description,
        EventType type,
        string createdBy,
        DateTime createdAt
    )
        : base(id)
    {
        EventDate = eventDate;
        Title = title;
        Description = description;
        Type = type;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public void Update(DateTime? eventDate, string? title, string? description, EventType? type)
    {
        if (eventDate.HasValue)
        {
            EventDate = eventDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            Description = description;
        }

        if (type.HasValue)
        {
            Type = type.Value;
        }
    }

    public void AddAttachment(EventAttachment attachment)
    {
        _attachments.Add(attachment);
    }

    public void RemoveAttachment(EventAttachmentId attachmentId)
    {
        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment != null)
        {
            _attachments.Remove(attachment);
        }
    }

    public bool IsUpcoming()
    {
        return EventDate > DateTime.UtcNow;
    }
}
