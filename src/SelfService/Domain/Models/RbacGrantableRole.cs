namespace SelfService.Domain.Models;

/// <summary>
/// A holder of <see cref="RoleId"/> on a capability may grant <see cref="GrantableRoleId"/> on that
/// capability. Callers with global rbac/create are not bound by these rules.
/// </summary>
public class RbacGrantableRole
{
    public RbacRoleId RoleId { get; private set; }
    public RbacRoleId GrantableRoleId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public RbacGrantableRole(RbacRoleId roleId, RbacRoleId grantableRoleId, DateTime createdAt)
    {
        RoleId = roleId;
        GrantableRoleId = grantableRoleId;
        CreatedAt = createdAt;
    }
}

public interface IRbacGrantableRoleRepository
{
    Task<List<RbacGrantableRole>> GetAll();

    /// <summary>Replaces every rule with <paramref name="rules"/>.</summary>
    Task ReplaceAll(IEnumerable<RbacGrantableRole> rules);
}
