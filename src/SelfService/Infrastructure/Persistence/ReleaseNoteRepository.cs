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
