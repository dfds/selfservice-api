using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacPermissionGrantRepository : GenericRepository<RbacPermissionGrant, RbacPermissionGrantId>, IRbacPermissionGrantRepository
{
    public RbacPermissionGrantRepository(SelfServiceDbContext dbContext) : base(dbContext.RbacPermissionGrants)
    {
        
    }
    
}