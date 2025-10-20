using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacRoleRepository : GenericRepository<RbacRole, RbacRoleId>, IRbacRoleRepository
{
    public RbacRoleRepository(SelfServiceDbContext dbContext)
        : base(dbContext.RbacRoles) { }
}
