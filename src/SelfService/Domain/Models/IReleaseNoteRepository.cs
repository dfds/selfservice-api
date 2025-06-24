namespace SelfService.Domain.Models;

public interface IReleaseNoteRepository
{
    Task Add(ReleaseNote releaseNote);
    Task<ReleaseNote> Get(ReleaseNoteId id);
    Task ToggleActive(ReleaseNoteId id);
    Task<IEnumerable<ReleaseNote>> GetAll();
}
