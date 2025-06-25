using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Domain.Services;

public class ReleaseNoteService : IReleaseNoteService
{
    private readonly ILogger<ECRRepositoryService> _logger;
    private readonly IReleaseNoteRepository _releaseNoteRepository;
    private SystemTime _systemTime;

    public ReleaseNoteService(
        ILogger<ECRRepositoryService> logger,
        IReleaseNoteRepository releaseNoteRepository,
        SystemTime systemTime
    )
    {
        _logger = logger;
        _releaseNoteRepository = releaseNoteRepository;
        _systemTime = systemTime;
    }

    [TransactionalBoundary]
    public async Task<ReleaseNote> AddReleaseNote(
        string title,
        string content,
        DateTime releaseDate,
        UserId createdBy,
        bool isActive = true
    )
    {
        var releaseNote = new ReleaseNote(
            ReleaseNoteId.New(),
            title,
            releaseDate,
            content,
            _systemTime.Now,
            createdBy.ToString(),
            _systemTime.Now, // modifiedAt is the same as createdAt for initial creation
            createdBy.ToString(), // modifiedBy is the same as createdBy for initial creation
            isActive
        );

        await _releaseNoteRepository.Add(releaseNote);
        return releaseNote;
    }

    [TransactionalBoundary]
    public async Task ToggleIsActive(ReleaseNoteId id)
    {
        await _releaseNoteRepository.ToggleActive(id);
    }

    public async Task<IEnumerable<ReleaseNote>> GetAllReleaseNotes()
    {
        return await _releaseNoteRepository.GetAll();
    }

    public async Task<ReleaseNote> GetReleaseNote(ReleaseNoteId id)
    {
        return await _releaseNoteRepository.Get(id);
    }
}
