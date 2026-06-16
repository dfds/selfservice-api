namespace SelfService.Domain.Models;

public interface IRbacRoleGrantRepository : IGenericRepository<RbacRoleGrant, RbacRoleGrantId>
{
    /// <summary>
    /// Bulk-loads role grants assigned directly to the given users in a single query.
    /// Avoids the N+1 of calling a per-user lookup in a loop.
    /// </summary>
    Task<List<RbacRoleGrant>> GetByAssignedUsers(IReadOnlyCollection<string> userIds);
}
