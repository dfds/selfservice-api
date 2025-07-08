using System.ComponentModel.DataAnnotations.Schema;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class ReleaseNote : AggregateRoot<ReleaseNoteId>
{
    public ReleaseNote(
        ReleaseNoteId id,
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
        ReleaseDate = releaseDate;
        Content = content;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
        IsActive = isActive;
        Version = version;
    }

    public static ReleaseNote CreateReleaseNote(
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
        var releaseNote = new ReleaseNote(
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

    public string Title { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string CreatedBy { get; set; }
    public string ModifiedBy { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }

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
