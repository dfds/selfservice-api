using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class MembershipRepository : IMembershipRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public MembershipRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Membership membership)
    {
        await _dbContext.Memberships.AddAsync(membership);
    }
}