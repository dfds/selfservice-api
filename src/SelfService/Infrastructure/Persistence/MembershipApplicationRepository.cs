using Microsoft.EntityFrameworkCore;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class MembershipApplicationRepository : IMembershipApplicationRepository
{
    private readonly SelfServiceDbContext _dbContext;
    private readonly SystemTime _systemTime;

    public MembershipApplicationRepository(SelfServiceDbContext dbContext, SystemTime systemTime)
    {
        _dbContext = dbContext;
        _systemTime = systemTime;
    }
    
    public async Task Add(MembershipApplication application)
    {
        await _dbContext.MembershipApplications.AddAsync(application);
    }

    public async Task<MembershipApplication> Get(MembershipApplicationId id)
    {
        var result = await _dbContext.MembershipApplications
            .Include(x => x.Approvals)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (result is null)
        {
            throw EntityNotFoundException<MembershipApplication>.UsingId(id);
        }

        return result;
    }

    public async Task<IEnumerable<MembershipApplication>> FindExpiredApplications()
    {
        var now = _systemTime.Now;
        return await _dbContext.MembershipApplications
            .Where(x => x.ExpiresOn <= now && x.Status == MempershipApplicationStatusOptions.PendingApprovals)
            .OrderBy(x => x.ExpiresOn)
            .ToListAsync();
    }
}