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

    public async Task<MembershipApplication?> FindBy(MembershipApplicationId id)
    {
        return await _dbContext.MembershipApplications
            .Include(x => x.Approvals)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<MembershipApplication>> FindExpiredApplications()
    {
        var now = _systemTime.Now;
        return await _dbContext.MembershipApplications
            .Include(x => x.Approvals)
            .Where(x => x.ExpiresOn <= now && x.Status == MembershipApplicationStatusOptions.PendingApprovals)
            .OrderBy(x => x.ExpiresOn)
            .ToListAsync();
    }

    public async Task<MembershipApplication?> FindPendingBy(CapabilityId capabilityId, UserId userId)
    {
        return await _dbContext.MembershipApplications
            .Include(x => x.Approvals)
            .Where(x => x.Status == MembershipApplicationStatusOptions.PendingApprovals &&
                        x.CapabilityId == capabilityId && x.Applicant == userId)
            .SingleOrDefaultAsync();
    }

    public async Task Remove(MembershipApplicationId id)
    {
        var application = await FindBy(id);
        if (application != null)
        {
            await Remove(application);
        }
    }

    public Task Remove(MembershipApplication application)
    {
        _dbContext.MembershipApplications.Remove(application);
        return Task.CompletedTask;
    }

    public async Task<List<MembershipApplication>> RemoveAllWithUserId(UserId userId)
    {
        var applications =  await _dbContext.MembershipApplications
            .Include(x => x.Applicant == userId)
            .ToListAsync();

        foreach (var membershipApplication in applications)
        {
            Remove(membershipApplication);
        }

        return applications;
    }
}