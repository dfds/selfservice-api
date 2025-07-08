using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class ReleaseNoteRepository : IReleaseNoteRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public ReleaseNoteRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReleaseNote> Get(ReleaseNoteId id)
    {
        var found = await _dbContext.ReleaseNotes.FindAsync(id);
        if (found is null)
        {
            throw EntityNotFoundException<ReleaseNote>.UsingId(id);
        }

        return found;
    }

    public async Task Add(ReleaseNote releaseNote)
    {
        await _dbContext.ReleaseNotes.AddAsync(releaseNote);
    }

    public async Task Update(ReleaseNoteId id, string title, string content, DateTime releaseDate, string modifiedBy)
    {
        var dbReleaseNote = await _dbContext.ReleaseNotes.FindAsync(id);
        if (dbReleaseNote is null)
        {
            throw EntityNotFoundException<ReleaseNote>.UsingId(id);
        }

        // Create ReleaseNoteHistory based on current version of ReleaseNote
        var releaseNoteHistory = new ReleaseNoteHistory(
            ReleaseNoteHistoryId.New(),
            dbReleaseNote.Id,
            dbReleaseNote.Title,
            dbReleaseNote.ReleaseDate,
            dbReleaseNote.Content,
            dbReleaseNote.CreatedAt,
            dbReleaseNote.CreatedBy,
            dbReleaseNote.ModifiedAt,
            dbReleaseNote.ModifiedBy,
            dbReleaseNote.IsActive,
            dbReleaseNote.Version
        );
        _dbContext.ReleaseNoteHistory.Add(releaseNoteHistory);

        // Update ReleaseNote, bump version
        dbReleaseNote.Title = title;
        dbReleaseNote.Version += 1;
        dbReleaseNote.Content = content;
        dbReleaseNote.ModifiedBy = modifiedBy;
        dbReleaseNote.ModifiedAt = DateTime.Now;
        dbReleaseNote.ReleaseDate = releaseDate;
        _dbContext.ReleaseNotes.Update(dbReleaseNote);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ReleaseNote>> GetAll()
    {
        return await _dbContext.ReleaseNotes.OrderBy(x => x.ReleaseDate).ToListAsync();
    }

    public async Task ToggleActive(ReleaseNoteId id)
    {
        var found = await _dbContext.ReleaseNotes.FindAsync(id);
        if (found is null)
        {
            throw EntityNotFoundException<ReleaseNote>.UsingId(id);
        }

        found.ToggleIsActive();

        _dbContext.ReleaseNotes.Update(found);
        await _dbContext.SaveChangesAsync();
    }
}
