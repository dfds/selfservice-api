using SelfService.Configuration;
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IRbacApplicationService
{
    Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForRoleGrants(List<RbacRoleGrant> roleGrants);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForUser(string user);
    Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user);

    /// <summary>
    /// Bulk variant of <see cref="GetRoleGrantsForUser"/>: loads role grants for many users in a single
    /// query and groups them by user id. Use this instead of calling the per-user method in a loop.
    /// </summary>
    Task<ILookup<string, RbacRoleGrant>> GetRoleGrantsForUsers(IReadOnlyCollection<string> userIds);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForGroup(string groupId);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForRole(string roleId);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForRoleIgnoreCase(string roleId);
    Task<List<RbacPermissionGrant>> GetPermissionGrantsForCapability(string capabilityId);
    Task<List<RbacRoleGrant>> GetRoleGrantsForCapability(string capabilityId);
    Task<List<RbacRoleGrant>> GetRoleGrantsForGroup(string groupId);
    Task<List<RbacGroup>> GetGroupsForUser(string user);
    Task<List<RbacGroup>> GetSystemGroups();
    Task<List<RbacRole>> GetAssignableRoles();
    Task<List<RbacRole>> GetAllRoles();
    Task GrantPermission(string user, RbacPermissionGrant permissionGrant);
    Task<BulkGrantResult<RbacPermissionGrant>> GrantPermissions(string user, List<RbacPermissionGrant> grants);
    Task RevokePermission(string user, string id);

    /// <param name="userInitiated">
    /// True when <paramref name="roleGrant"/> came from a caller-supplied request body rather than a
    /// platform workflow. Enables authorization through <see cref="CanGrantRoleGrant"/>. Defaults to
    /// false, which preserves the trusted behaviour used by capability bootstrap, membership approval,
    /// bulk import and the global /rbac endpoints.
    /// </param>
    Task GrantRoleGrant(string user, RbacRoleGrant roleGrant, bool userInitiated = false);

    /// <summary>
    /// Global rbac/create holders may grant anything. Otherwise the grant must be capability-scoped, not
    /// to the caller, and both its role and the role of the grant it replaces must be grantable by the
    /// caller on that capability according to the grant matrix.
    /// </summary>
    Task<bool> CanGrantRoleGrant(string user, RbacRoleGrant roleGrant);

    /// <summary>Capability roles the caller may grant on <paramref name="capabilityId"/>.</summary>
    Task<List<RbacRole>> GetGrantableRoles(string user, string capabilityId);

    /// <summary>Owner, Contributor, Reader and roles of type Capability; never Guest.</summary>
    Task<List<RbacRole>> GetCapabilityRoles();
    Task<List<RbacGrantableRole>> GetGrantMatrix();

    /// <summary>Replaces the grant matrix. Requires global rbac/create; both ends must be capability roles.</summary>
    Task SetGrantMatrix(string user, List<(RbacRoleId RoleId, RbacRoleId GrantableRoleId)> rules);
    Task<bool> CanManageGrantMatrix(string user);
    Task<BulkGrantResult<RbacRoleGrant>> GrantRoleGrants(string user, List<RbacRoleGrant> grants);
    Task<RbacRoleGrant?> RevokeRoleGrant(string user, string id);
    Task RevokeCapabilityRoleGrant(UserId userId, CapabilityId capabilityId);
    Task<RbacRole> CreateRole(string user, RbacRole role);
    Task DeleteRole(string user, string roleId);
    Task<RbacGroup> CreateGroup(string user, RbacGroup group);
    Task DeleteGroup(string user, string groupId);
    Task<RbacGroupMember> GrantGroupGrant(string user, RbacGroupMember membership);
    Task RevokeGroupGrant(string user, RbacGroupMember membership);
    Task SetPermissionsForRole(string roleId, List<RolePermissionEntry> permissions);
}

public class BulkGrantResult<TInput>
{
    public List<TInput> CreatedInputs { get; set; } = new();
    public List<(TInput Input, string Reason)> Failed { get; set; } = new();
}
