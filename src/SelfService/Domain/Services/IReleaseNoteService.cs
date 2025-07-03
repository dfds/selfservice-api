using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IReleaseNoteService
{
    Task<IEnumerable<ReleaseNote>> GetAllReleaseNotes();
    Task<ReleaseNote> GetReleaseNote(ReleaseNoteId id);
    Task<ReleaseNote> AddReleaseNote(
        string title,
        string content,
        DateTime releaseDate,
        UserId createdBy,
        int version,
        bool isActive = true
    );

    Task UpdateReleaseNote(ReleaseNoteId id, string title, string content, DateTime releaseDate, string modifiedBy);
    Task ToggleIsActive(ReleaseNoteId id);
}
