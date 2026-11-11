using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacGrantableRoleRepository : IRbacGrantableRoleRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public RbacGrantableRoleRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<RbacGrantableRole>> GetAll()
    {
        return _dbContext.RbacGrantableRoles.ToListAsync();
    }

    // Applied as a diff: removing and re-adding a row with the same composite key in one context
    // makes EF track two instances with one key.
    public async Task ReplaceAll(IEnumerable<RbacGrantableRole> rules)
    {
        var desired = rules.ToList();
        var existing = await _dbContext.RbacGrantableRoles.ToListAsync();

        _dbContext.RbacGrantableRoles.RemoveRange(
            existing.Where(e => !desired.Any(d => d.RoleId == e.RoleId && d.GrantableRoleId == e.GrantableRoleId))
        );
        await _dbContext.RbacGrantableRoles.AddRangeAsync(
            desired.Where(d => !existing.Any(e => e.RoleId == d.RoleId && e.GrantableRoleId == d.GrantableRoleId))
        );
    }
}
