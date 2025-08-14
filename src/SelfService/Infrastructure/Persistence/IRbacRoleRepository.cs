using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public interface IRbacRoleRepository : IGenericRepository<RbacRole, RbacRoleId>
{
    
}