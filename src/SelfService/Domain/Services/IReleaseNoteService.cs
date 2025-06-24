using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IReleaseNoteService
{
    Task<IEnumerable<ReleaseNote>> GetAllReleaseNotes();
    Task<ReleaseNote> AddReleaseNote(
        string title,
        string content,
        DateTime releaseDate,
        UserId createdBy,
        bool isActive = true
    );
    Task ToggleIsActive(ReleaseNoteId id);
}
