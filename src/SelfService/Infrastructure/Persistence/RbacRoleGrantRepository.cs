using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacRoleGrantRepository : GenericRepository<RbacRoleGrant, RbacRoleGrantId>, IRbacRoleGrantRepository
{
    public RbacRoleGrantRepository(SelfServiceDbContext dbContext) : base(dbContext.RbacRoleGrants)
    {
        
    }
    
}