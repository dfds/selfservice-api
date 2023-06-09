using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence.Queries;

public class MembershipApplicationQuery : IMembershipApplicationQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public MembershipApplicationQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MembershipApplication?> FindById(MembershipApplicationId id)
    {
        return _dbContext.MembershipApplications.SingleOrDefaultAsync(x => x.Id == id);
    }
}