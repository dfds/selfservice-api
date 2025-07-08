namespace SelfService.Domain.Models;

public interface IReleaseNoteRepository
{
    Task Add(ReleaseNote releaseNote);
    Task Update(ReleaseNoteId id, string title, string content, DateTime releaseDate, string modifiedBy);
    Task<ReleaseNote> Get(ReleaseNoteId id);
    Task ToggleActive(ReleaseNoteId id);
    Task<IEnumerable<ReleaseNote>> GetAll();
}
