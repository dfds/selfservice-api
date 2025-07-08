using System.ComponentModel.DataAnnotations.Schema;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class ReleaseNoteHistory : AggregateRoot<ReleaseNoteHistoryId>
{
    public ReleaseNoteHistory(
        ReleaseNoteHistoryId id,
        ReleaseNoteId releaseNoteId,
        string title,
        DateTime releaseDate,
        string content,
        DateTime createdAt,
        string createdBy,
        DateTime modifiedAt,
        string modifiedBy,
        bool isActive,
        int version
    )
        : base(id)
    {
        Title = title;
        ReleaseNoteId = releaseNoteId;
        ReleaseDate = releaseDate;
        Content = content;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
        IsActive = isActive;
        Version = version;
    }

    public static ReleaseNoteHistory CreateReleaseNoteHistory(
        ReleaseNoteHistoryId releaseNoteHistoryId,
        ReleaseNoteId releaseNoteId,
        string title,
        DateTime releaseDate,
        string content,
        DateTime creationTime,
        string createdBy,
        DateTime modifiedAt,
        string modifiedBy,
        bool isActive,
        int version
    )
    {
        var releaseNote = new ReleaseNoteHistory(
            releaseNoteHistoryId,
            releaseNoteId,
            title,
            releaseDate,
            content,
            creationTime,
            createdBy,
            modifiedAt,
            modifiedBy,
            isActive,
            version
        );
        return releaseNote;
    }

    public string Title { get; private set; }
    public ReleaseNoteId ReleaseNoteId { get; private set; }
    public DateTime ReleaseDate { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ModifiedAt { get; private set; }
    public string CreatedBy { get; private set; }
    public string ModifiedBy { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public override string ToString()
    {
        return Id.ToString();
    }

    /* // For once I don't think we actually need events??
    public void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }
    */

    public void ToggleIsActive()
    {
        IsActive = !IsActive;
    }
}
