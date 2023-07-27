using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class PortalVisitRepository : IPortalVisitRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public PortalVisitRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(PortalVisit portalVisit)
    {
        await _dbContext.PortalVisits.AddAsync(portalVisit);
    }
}