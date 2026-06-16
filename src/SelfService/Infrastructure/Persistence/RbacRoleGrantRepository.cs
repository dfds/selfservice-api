using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacRoleGrantRepository : GenericRepository<RbacRoleGrant, RbacRoleGrantId>, IRbacRoleGrantRepository
{
    public RbacRoleGrantRepository(SelfServiceDbContext dbContext)
        : base(dbContext.RbacRoleGrants) { }

    public Task<List<RbacRoleGrant>> GetByAssignedUsers(IReadOnlyCollection<string> userIds)
    {
        if (userIds.Count == 0)
            return Task.FromResult(new List<RbacRoleGrant>());

        return DbSetReference
            .Where(g => g.AssignedEntityType == AssignedEntityType.User && userIds.Contains(g.AssignedEntityId))
            .ToListAsync();
    }
}
