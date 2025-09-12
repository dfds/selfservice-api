using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class RbacGroupRepository : GenericRepository<RbacGroup, RbacGroupId>, IRbacGroupRepository
{
    public RbacGroupRepository(SelfServiceDbContext dbContext)
        : base(dbContext.RbacGroups) { }
}
