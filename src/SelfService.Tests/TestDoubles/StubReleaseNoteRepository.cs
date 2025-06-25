using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubReleaseNoteRepository : IReleaseNoteRepository
{
    private readonly ReleaseNote? _releaseNote;

    public StubReleaseNoteRepository(ReleaseNote? releaseNote = null)
    {
        _releaseNote = releaseNote;
    }

    public Task<ReleaseNote> Get(ReleaseNoteId id)
    {
        return Task.FromResult(_releaseNote!);
    }

    public Task<ReleaseNote?> FindBy(ReleaseNoteId id)
    {
        return Task.FromResult(_releaseNote);
    }

    public Task<bool> Exists(ReleaseNoteId id)
    {
        return Task.FromResult(_releaseNote != null);
    }

    public Task Add(ReleaseNote releaseNote)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ReleaseNote>> GetAll()
    {
        return Task.FromResult<IEnumerable<ReleaseNote>>(
            _releaseNote != null ? new[] { _releaseNote } : Array.Empty<ReleaseNote>()
        );
    }

    public Task ToggleActive(ReleaseNoteId id)
    {
        throw new NotImplementedException();
    }
}
