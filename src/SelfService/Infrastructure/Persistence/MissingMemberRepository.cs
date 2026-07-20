using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class MissingMemberRepository : IMissingMemberRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public MissingMemberRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MissingMemberRecord?> FindByUser(string userId)
    {
        return await _dbContext.MissingMemberRecords.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task Add(MissingMemberRecord record)
    {
        await _dbContext.MissingMemberRecords.AddAsync(record);
    }

    public async Task Update(MissingMemberRecord record)
    {
        _dbContext.MissingMemberRecords.Update(record);
        await Task.CompletedTask;
    }

    public async Task RemoveByUserId(string userId)
    {
        await _dbContext.MissingMemberRecords.Where(x => x.UserId == userId).ExecuteDeleteAsync();
    }
}
