using SelfService.Configuration;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class RbacApplicationService : IRbacApplicationService
{
    private readonly IRbacPermissionGrantRepository _permissionGrantRepository;
    private readonly IRbacRoleGrantRepository _roleGrantRepository;
    private readonly IRbacGroupMemberRepository _groupMemberRepository;
    private readonly IRbacGroupRepository _groupRepository;
    private readonly IPermissionQuery _permissionQuery;
    private readonly IRbacRoleRepository _roleRepository;
    private readonly RbacCache _cache;

    public RbacApplicationService(
        IRbacPermissionGrantRepository permissionGrantRepository,
        IRbacRoleGrantRepository roleGrantRepository,
        IRbacGroupMemberRepository groupMemberRepository,
        IRbacGroupRepository groupRepository,
        IPermissionQuery permissionQuery,
        IRbacRoleRepository roleRepository
    )
    {
        _permissionGrantRepository = permissionGrantRepository;
        _roleGrantRepository = roleGrantRepository;
        _groupMemberRepository = groupMemberRepository;
        _groupRepository = groupRepository;
        _permissionQuery = permissionQuery;
        _roleRepository = roleRepository;
        _cache = new RbacCache();
    }

    public async Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId)
    {
        var resp = new PermittedResponse();
        permissions.ForEach(p => resp.PermissionMatrix.Add($"{p.Namespace}-{p.Name}", new PermissionMatrix(p)));

        // user level
        var userPermissions = await GetPermissionGrantsForUser(user);
        var userRoles = await GetRoleGrantsForUser(user);

        // group level
        var groupPermissions = await _cache.GetOrAddAsync(
            CacheConst.UserGroupPermissions,
            user,
            () => _permissionQuery.FindUserGroupPermissionsByUserId(user)
        );
        var groupRoles = await _cache.GetOrAddAsync(
            CacheConst.UserGroupRoles,
            user,
            () => _permissionQuery.FindUserGroupRolesByUserId(user)
        );

        var combinedPermissions = userPermissions.Concat(groupPermissions).ToList();
        var combinedRoles = userRoles.Concat(groupRoles).ToList();

        // If the user has no explicit capability role for this resource, apply Guest role permissions as default
        var isCapabilityCheck = permissions.Any(p => p.AccessType == RbacAccessType.Capability);
        if (
            isCapabilityCheck
            && !combinedRoles.Any(rg => rg.Type == RbacAccessType.Capability && rg.Resource == objectId)
        )
        {
            var guestPermissions = await _cache.GetOrAddAsync(
                CacheConst.GuestPermissions,
                "global",
                () => _permissionQuery.FindGuestPermissions()
            );
            combinedPermissions = combinedPermissions
                .Concat(
                    guestPermissions.Select(p => new RbacPermissionGrant(
                        p.Id,
                        p.CreatedAt,
                        p.AssignedEntityType,
                        p.AssignedEntityId,
                        p.Namespace,
                        p.Permission,
                        RbacAccessType.Capability,
                        objectId
                    ))
                )
                .ToList();
        }

        var accessGrantingPermissionGrants = combinedPermissions.FindAll(p =>
        {
            var policyGrantsAccess = false;
            if (p.Type != RbacAccessType.Global && (p.Resource is null || !p.Resource.Equals(objectId)))
            {
                return false;
            }

            permissions.ForEach(pm =>
            {
                if (pm.Namespace == p.Namespace && pm.Name == p.Permission)
                {
                    resp.PermissionMatrix[$"{p.Namespace}-{p.Permission}"].Permitted = true;
                    policyGrantsAccess = true;
                }
            });

            return policyGrantsAccess;
        });
        resp.PermissionGrants = accessGrantingPermissionGrants;

        // New - handling mapping roles to permissions
        var permissionsFromRoles = await GetPermissionGrantsForRoleGrants(combinedRoles);

        accessGrantingPermissionGrants = permissionsFromRoles.FindAll(p =>
        {
            var policyGrantsAccess = false;
            if (p.Type != RbacAccessType.Global && (p.Resource is null || !p.Resource.Equals(objectId)))
            {
                return false;
            }

            permissions.ForEach(pm =>
            {
                if (pm.Namespace == p.Namespace && pm.Name == p.Permission)
                {
                    resp.PermissionMatrix[$"{p.Namespace}-{p.Permission}"].Permitted = true;
                    policyGrantsAccess = true;
                }
            });

            return policyGrantsAccess;
        });
        resp.PermissionGrants.AddRange(accessGrantingPermissionGrants);

        return resp;
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForRoleGrants(List<RbacRoleGrant> roleGrants)
    {
        var payload = new List<RbacPermissionGrant>();
        foreach (var rg in roleGrants)
        {
            var foundPermissions = await GetPermissionGrantsForRoleIgnoreCase(rg.RoleId.ToString());
            var modifiedPermissions = new List<RbacPermissionGrant>();

            // override type & resource from grant to permission
            foreach (var rbacPermissionGrant in foundPermissions)
            {
                var modifiedPermission = new RbacPermissionGrant(
                    rbacPermissionGrant.Id,
                    rbacPermissionGrant.CreatedAt,
                    rbacPermissionGrant.AssignedEntityType,
                    rbacPermissionGrant.AssignedEntityId,
                    rbacPermissionGrant.Namespace,
                    rbacPermissionGrant.Permission,
                    rg.Type,
                    rg.Resource!
                );
                modifiedPermissions.Add(modifiedPermission);
            }

            payload.AddRange(modifiedPermissions);
        }

        return payload;
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForUser(string user)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.PermissionGrantsForUser,
            user,
            () =>
                _permissionGrantRepository.GetAllWithPredicate(p =>
                    p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user
                )
        );
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.RoleGrantsForUser,
            user,
            () =>
                _roleGrantRepository.GetAllWithPredicate(p =>
                    p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user
                )
        );
    }

    public async Task<ILookup<string, RbacRoleGrant>> GetRoleGrantsForUsers(IReadOnlyCollection<string> userIds)
    {
        if (userIds.Count == 0)
            return Enumerable.Empty<RbacRoleGrant>().ToLookup(g => g.AssignedEntityId);

        var grants = await _roleGrantRepository.GetByAssignedUsers(userIds);
        return grants.ToLookup(g => g.AssignedEntityId);
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForGroup(string groupId)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.PermissionGrantsForGroup,
            groupId,
            () =>
                _permissionGrantRepository.GetAllWithPredicate(p =>
                    p.AssignedEntityType == AssignedEntityType.Group && p.AssignedEntityId == groupId
                )
        );
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForRole(string roleId)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.PermissionGrantsForRole,
            roleId,
            () =>
                _permissionGrantRepository.GetAllWithPredicate(p =>
                    p.AssignedEntityType == AssignedEntityType.Role && p.AssignedEntityId == roleId
                )
        );
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForRoleIgnoreCase(string roleId)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.PermissionGrantsForRoleIgnoreCase,
            roleId,
            () =>
                _permissionGrantRepository.GetAllWithPredicate(p =>
                    p.AssignedEntityType == AssignedEntityType.Role
                    && string.Equals(p.AssignedEntityId, roleId, StringComparison.CurrentCultureIgnoreCase)
                )
        );
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForCapability(string capabilityId)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.PermissionGrantsForCapability,
            capabilityId,
            () =>
                _permissionGrantRepository.GetAllWithPredicate(p =>
                    p.Type == RbacAccessType.Capability && p.Resource == capabilityId
                )
        );
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForCapability(string capabilityId)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.RoleGrantsForCapability,
            capabilityId,
            () =>
                _roleGrantRepository.GetAllWithPredicate(p =>
                    p.Type == RbacAccessType.Capability && p.Resource == capabilityId
                )
        );
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForGroup(string groupId)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.RoleGrantsForGroup,
            groupId,
            () =>
                _roleGrantRepository.GetAllWithPredicate(p =>
                    p.AssignedEntityType == AssignedEntityType.Group && p.AssignedEntityId == groupId
                )
        );
    }

    public async Task<List<RbacGroup>> GetGroupsForUser(string user)
    {
        return await _cache.GetOrAddAsync(
            CacheConst.GroupsForUser,
            user,
            () => _groupRepository.GetAllWithPredicate(x => x.Members.Any(m => m.UserId == user))
        );
    }

    private async Task<List<RbacRole>> GetAllRolesInternal()
    {
        return await _cache.GetOrAddAsync(CacheConst.AssignableRoles, "all", () => _roleRepository.GetAll());
    }

    public async Task<List<RbacRole>> GetAllRoles()
    {
        return await GetAllRolesInternal();
    }

    // Returns roles that can be assigned to capability members. Guest is excluded as it is a system-level
    // default role applied implicitly to users without an explicit capability role.
    public async Task<List<RbacRole>> GetAssignableRoles()
    {
        var allRoles = await GetAllRolesInternal();
        return allRoles.Where(r => r.Name != "Guest").ToList();
    }

    [TransactionalBoundary]
    public async Task GrantPermission(string user, RbacPermissionGrant permissionGrant)
    {
        //PermittedResponse? canUserCreateGlobalRbac;
        switch (permissionGrant.Type)
        {
            case var a when a == RbacAccessType.Global:
                /*
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
                    permissionGrant.Resource ?? ""
                );
                if (!canUserCreateGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                */
                await _permissionGrantRepository.Add(
                    RbacPermissionGrant.New(
                        permissionGrant.AssignedEntityType,
                        permissionGrant.AssignedEntityId,
                        permissionGrant.Namespace,
                        permissionGrant.Permission,
                        permissionGrant.Type,
                        permissionGrant.Resource ?? ""
                    )
                );

                break;
            case var a when a == RbacAccessType.Capability:
                /*
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
                    permissionGrant.Resource ?? ""
                );
                var canUserCreateCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new(RbacNamespace.CapabilityManagement, "manage-permissions", "", RbacAccessType.Capability),
                    },
                    permissionGrant.Resource ?? ""
                );

                if (!canUserCreateGlobalRbac.Permitted() && !canUserCreateCapabilityRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                */
                await _permissionGrantRepository.Add(
                    RbacPermissionGrant.New(
                        permissionGrant.AssignedEntityType,
                        permissionGrant.AssignedEntityId,
                        permissionGrant.Namespace,
                        permissionGrant.Permission,
                        permissionGrant.Type,
                        permissionGrant.Resource ?? ""
                    )
                );

                break;
            default:
                throw new Exception("Invalid permission grant");
        }
        _cache.Reset();
    }

    [TransactionalBoundary]
    public async Task<BulkGrantResult<RbacPermissionGrant>> GrantPermissions(
        string user,
        List<RbacPermissionGrant> grants
    )
    {
        var result = new BulkGrantResult<RbacPermissionGrant>();
        foreach (var grant in grants)
        {
            await GrantPermission(user, grant);
            result.CreatedInputs.Add(grant);
        }
        return result;
    }

    [TransactionalBoundary]
    public async Task RevokePermission(string user, string id)
    {
        _cache.Reset();
        var permissionLookup = await _permissionGrantRepository.FindById(RbacPermissionGrantId.Parse(id));
        if (permissionLookup == null)
            throw new Exception("Permission grant not found");

        PermittedResponse? canUserCreateGlobalRbac;
        switch (permissionLookup.Type)
        {
            case var a when a == RbacAccessType.Global:

                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "delete", "", RbacAccessType.Global) },
                    permissionLookup.Resource ?? ""
                );
                if (!canUserCreateGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }

                await _permissionGrantRepository.Remove(permissionLookup.Id);
                break;
            case var a when a == RbacAccessType.Capability:
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "delete", "", RbacAccessType.Global) },
                    permissionLookup.Resource ?? ""
                );
                var canUserCreateCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new(RbacNamespace.CapabilityManagement, "manage-permissions", "", RbacAccessType.Capability),
                    },
                    permissionLookup.Resource ?? ""
                );

                if (!canUserCreateGlobalRbac.Permitted() && !canUserCreateCapabilityRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                await _permissionGrantRepository.Remove(permissionLookup.Id);
                break;
            default:
                throw new Exception("Invalid permission grant");
        }
        _cache.Reset();
    }

    [TransactionalBoundary]
    public async Task GrantRoleGrant(string user, RbacRoleGrant roleGrant)
    {
        //PermittedResponse? canUserCreateGlobalRbac;
        switch (roleGrant.Type)
        {
            case var a when a == RbacAccessType.Global:
                /*
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                if (!canUserCreateGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                */
                await _roleGrantRepository.Add(
                    RbacRoleGrant.New(
                        roleGrant.RoleId,
                        roleGrant.AssignedEntityType,
                        roleGrant.AssignedEntityId,
                        roleGrant.Type,
                        roleGrant.Resource ?? ""
                    )
                );

                break;
            case var a when a == RbacAccessType.Capability:
                /*
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                var canUserCreateCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new(RbacNamespace.CapabilityManagement, "manage-permissions", "", RbacAccessType.Capability),
                    },
                    roleGrant.Resource ?? ""
                );
                var userGrantsToSelf = canUserCreateGlobalRbac.Permitted()
                    ? false
                    : user == roleGrant.AssignedEntityId && roleGrant.AssignedEntityType == AssignedEntityType.User;

                if (
                    (!canUserCreateGlobalRbac.Permitted() && !canUserCreateCapabilityRbac.Permitted())
                    || userGrantsToSelf
                )
                {
                    throw new UnauthorizedAccessException();
                }
                */
                if (roleGrant.Resource == null)
                {
                    throw new BadHttpRequestException("Capability ID is required for capability role grants");
                }

                var guestRoleCheck = (await GetAllRolesInternal()).FirstOrDefault(r => r.Name == "Guest");
                if (guestRoleCheck != null && roleGrant.RoleId == guestRoleCheck.Id)
                {
                    throw new BadHttpRequestException(
                        "Guest role cannot be directly assigned to a capability. It is the implicit default role for users without an explicit capability role."
                    );
                }

                var existingCapabilityGrant = await _roleGrantRepository.FindByPredicate(rg =>
                    rg.AssignedEntityId == roleGrant.AssignedEntityId
                    && rg.Resource == roleGrant.Resource
                    && rg.Type == roleGrant.Type
                );
                if (existingCapabilityGrant != null)
                {
                    await _roleGrantRepository.Remove(existingCapabilityGrant.Id);
                }
                await _roleGrantRepository.Add(
                    RbacRoleGrant.New(
                        roleGrant.RoleId,
                        roleGrant.AssignedEntityType,
                        roleGrant.AssignedEntityId,
                        roleGrant.Type,
                        roleGrant.Resource
                    )
                );

                break;
            default:
                throw new Exception("Invalid role grant");
        }
        _cache.Reset();
    }

    [TransactionalBoundary]
    public async Task<BulkGrantResult<RbacRoleGrant>> GrantRoleGrants(string user, List<RbacRoleGrant> grants)
    {
        var result = new BulkGrantResult<RbacRoleGrant>();
        foreach (var grant in grants)
        {
            await GrantRoleGrant(user, grant);
            result.CreatedInputs.Add(grant);
        }
        return result;
    }

    [TransactionalBoundary]
    public async Task<RbacRoleGrant?> RevokeRoleGrant(string user, string id)
    {
        _cache.Reset();
        var roleGrant = await _roleGrantRepository.FindById(RbacRoleGrantId.Parse(id));
        if (roleGrant == null)
            throw new Exception("Role grant not found");

        PermittedResponse? canUserDeleteGlobalRbac;
        switch (roleGrant.Type)
        {
            case var a when a == RbacAccessType.Global:

                canUserDeleteGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "delete", "", RbacAccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                if (!canUserDeleteGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }

                await _roleGrantRepository.Remove(roleGrant.Id);
                break;
            case var a when a == RbacAccessType.Capability:

                canUserDeleteGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "delete", "", RbacAccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                var canUserDeleteCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new(RbacNamespace.CapabilityManagement, "manage-permissions", "", RbacAccessType.Capability),
                    },
                    roleGrant.Resource ?? ""
                );

                if (!canUserDeleteGlobalRbac.Permitted() && !canUserDeleteCapabilityRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                await _roleGrantRepository.Remove(roleGrant.Id);
                break;
            default:
                throw new Exception("Invalid role grant");
        }
        _cache.Reset();
        return roleGrant;
    }

    [TransactionalBoundary]
    public async Task RevokeCapabilityRoleGrant(UserId userId, CapabilityId capabilityId)
    {
        _cache.Reset();
        var existingCapabilityGrant = await _roleGrantRepository.FindByPredicate(rg =>
            rg.AssignedEntityId == userId.ToString() && rg.Resource == capabilityId.ToString()
        );
        if (existingCapabilityGrant != null)
        {
            await _roleGrantRepository.Remove(existingCapabilityGrant.Id);
            _cache.Reset();
        }
    }

    [TransactionalBoundary]
    public async Task<RbacRole> CreateRole(string user, RbacRole role)
    {
        /*
        var canUserCreateGlobalRbac = await IsUserPermitted(
            user,
            new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
            ""
        );
        if (!canUserCreateGlobalRbac.Permitted())
        {
            throw new UnauthorizedAccessException();
        }
        */

        var newRole = RbacRole.New(
            ownerId: role.OwnerId,
            name: role.Name,
            description: role.Description,
            type: role.Type
        );
        await _roleRepository.Add(newRole);
        _cache.Reset();

        return newRole;
    }

    [TransactionalBoundary]
    public async Task<List<RbacGroup>> GetSystemGroups()
    {
        return await _cache.GetOrAddAsync(CacheConst.SystemGroups, "all", () => _groupRepository.GetAll());
    }

    [TransactionalBoundary]
    public async Task DeleteRole(string user, string roleId)
    {
        _cache.Reset();
        var canUserDeleteGlobalRbac = await IsUserPermitted(
            user,
            new List<Permission> { new(RbacNamespace.Rbac, "delete", "", RbacAccessType.Global) },
            ""
        );
        if (!canUserDeleteGlobalRbac.Permitted())
        {
            throw new UnauthorizedAccessException();
        }

        var role = await _roleRepository.FindById(RbacRoleId.Parse(roleId));
        if (role == null)
            throw new Exception("Role not found");

        await _roleRepository.Remove(role.Id);
        _cache.Reset();
    }

    [TransactionalBoundary]
    public async Task<RbacGroup> CreateGroup(string user, RbacGroup group)
    {
        /*
        var canUserCreateGlobalRbac = await IsUserPermitted(
            user,
            new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
            ""
        );
        if (!canUserCreateGlobalRbac.Permitted())
        {
            throw new UnauthorizedAccessException();
        }
        */

        var newGroup = RbacGroup.New(name: group.Name, description: group.Description, members: group.Members);
        await _groupRepository.Add(newGroup);
        _cache.Reset();
        return newGroup;
    }

    [TransactionalBoundary]
    public async Task DeleteGroup(string user, string groupId)
    {
        _cache.Reset();
        var canUserDeleteGlobalRbac = await IsUserPermitted(
            user,
            new List<Permission> { new(RbacNamespace.Rbac, "delete", "", RbacAccessType.Global) },
            ""
        );
        if (!canUserDeleteGlobalRbac.Permitted())
        {
            throw new UnauthorizedAccessException();
        }
        var group = await _groupRepository.FindById(RbacGroupId.Parse(groupId));
        if (group == null)
            throw new Exception("Group not found");

        await _groupRepository.Remove(group.Id);
        _cache.Reset();
    }

    [TransactionalBoundary]
    public async Task<RbacGroupMember> GrantGroupGrant(string user, RbacGroupMember membership)
    {
        /*
        var canUserCreateGlobalRbac = await IsUserPermitted(
            user,
            new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
            ""
        );
        if (!canUserCreateGlobalRbac.Permitted())
        {
            throw new UnauthorizedAccessException();
        }
        */

        var group = await _groupRepository.FindById(RbacGroupId.Parse(membership.GroupId));
        if (group == null)
        {
            throw new Exception("Group not found");
        }

        await _groupMemberRepository.Add(membership);
        _cache.Reset();
        return membership;
    }

    [TransactionalBoundary]
    public async Task RevokeGroupGrant(string user, RbacGroupMember membership)
    {
        _cache.Reset();
        var canUserDeleteGlobalRbac = await IsUserPermitted(
            user,
            new List<Permission> { new(RbacNamespace.Rbac, "delete", "", RbacAccessType.Global) },
            ""
        );
        if (!canUserDeleteGlobalRbac.Permitted())
        {
            throw new UnauthorizedAccessException();
        }

        var group = await _groupRepository.FindById(RbacGroupId.Parse(membership.GroupId));
        if (group == null)
            throw new Exception("Group not found");

        // The controller passes a freshly-minted RbacGroupMember (random Id) carrying
        // only the GroupId + UserId of the membership the caller wants to revoke.
        // Look up the real membership row before removing.
        var existing = await _groupMemberRepository.FindByPredicate(m =>
            m.GroupId == membership.GroupId && m.UserId == membership.UserId
        );
        if (existing == null)
            throw EntityNotFoundException<RbacGroupMemberId>.UsingId(
                $"(groupId={membership.GroupId}, userId={membership.UserId})"
            );

        await _groupMemberRepository.Remove(existing.Id);
        _cache.Reset();
    }

    public async Task<bool> CanModifyCapabilityRbac(string user, string id)
    {
        var resp = await IsUserPermitted(
            user,
            new List<Permission>
            {
                new()
                {
                    Namespace = RbacNamespace.CapabilityManagement,
                    Name = "manage-permissions",
                    AccessType = RbacAccessType.Global,
                },
                new()
                {
                    Namespace = RbacNamespace.Rbac,
                    Name = "create",
                    AccessType = RbacAccessType.Global,
                },
                new()
                {
                    Namespace = RbacNamespace.Rbac,
                    Name = "update",
                    AccessType = RbacAccessType.Global,
                },
                new()
                {
                    Namespace = RbacNamespace.Rbac,
                    Name = "delete",
                    AccessType = RbacAccessType.Global,
                },
            },
            id
        );

        return resp.PermissionMatrix.Any(x => x.Value.Permitted);
    }

    [TransactionalBoundary]
    public async Task SetPermissionsForRole(string roleId, List<RolePermissionEntry> permissions)
    {
        var existing = await GetPermissionGrantsForRoleIgnoreCase(roleId);
        foreach (var grant in existing)
            await _permissionGrantRepository.Remove(grant.Id);

        foreach (var p in permissions)
            await _permissionGrantRepository.Add(
                RbacPermissionGrant.New(
                    AssignedEntityType.Role,
                    roleId,
                    p.Namespace,
                    p.PermissionName,
                    p.AccessType,
                    ""
                )
            );

        _cache.Reset();
    }
}

public class Permission
{
    public String Description { get; set; } = "";
    public String Name { get; set; } = "";
    public RbacNamespace Namespace { get; set; } = RbacNamespace.Default;
    public RbacAccessType AccessType { get; set; } = RbacAccessType.Capability;

    // Canonical catalog of known RBAC permissions exposed by the application.
    // The database stores the actual permission grants (who/what has which permission and scope).
    public static List<Permission> BootstrapPermissions()
    {
        var permissions = new List<Permission>
        {
            new(RbacNamespace.Topics, "create", "Create new topics", RbacAccessType.Capability),
            new(RbacNamespace.Topics, "read-private", "Read private topics", RbacAccessType.Capability),
            new(RbacNamespace.Topics, "read-public", "Read public topics", RbacAccessType.Capability),
            new(RbacNamespace.Topics, "update", "Update topics", RbacAccessType.Capability),
            new(RbacNamespace.Topics, "delete", "Delete topics", RbacAccessType.Capability),
            new(RbacNamespace.CapabilityManagement, "receive-alerts", "Receive Alarms", RbacAccessType.Capability),
            new(
                RbacNamespace.CapabilityManagement,
                "receive-cost",
                "Receive cost summary reports",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.CapabilityManagement,
                "request-deletion",
                "Request Capability deletion",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.CapabilityManagement,
                "manage-permissions",
                "Manage Capability permissions",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.CapabilityManagement,
                "read-self-assess",
                "Self assessment permissions",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.CapabilityManagement,
                "create-self-assess",
                "Self assessment permissions",
                RbacAccessType.Capability
            ),
            new(RbacNamespace.CapabilityMembershipManagement, "create", "Invite new member", RbacAccessType.Capability),
            new(RbacNamespace.CapabilityMembershipManagement, "delete", "Remove member", RbacAccessType.Capability),
            new(RbacNamespace.CapabilityMembershipManagement, "read", "See member list", RbacAccessType.Capability),
            new(
                RbacNamespace.CapabilityMembershipManagement,
                "read-requests",
                "Read invitation/application requests",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.CapabilityMembershipManagement,
                "manage-requests",
                "Approve/decline member requests",
                RbacAccessType.Capability
            ),
            new(RbacNamespace.TagsAndMetadata, "create", "Create", RbacAccessType.Capability),
            new(RbacNamespace.TagsAndMetadata, "read", "Read", RbacAccessType.Capability),
            new(RbacNamespace.TagsAndMetadata, "update", "Update", RbacAccessType.Capability),
            new(RbacNamespace.TagsAndMetadata, "delete", "Delete", RbacAccessType.Capability),
            new(RbacNamespace.Aws, "create", "Create context/cloud resources", RbacAccessType.Capability),
            new(RbacNamespace.Aws, "read", "Read context/cloud resources", RbacAccessType.Capability),
            new(RbacNamespace.Aws, "manage-provider", "Read resources in AWS account", RbacAccessType.Capability),
            new(RbacNamespace.Aws, "read-provider", "Manage resources in AWS account", RbacAccessType.Capability),
            new(RbacNamespace.Finout, "read-dashboards", "See all DFDS dashboards", RbacAccessType.Global),
            new(
                RbacNamespace.Finout,
                "manage-dashboards",
                "Manage dashboard with Capability prefix",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.Finout,
                "manage-alerts",
                "Manage anomaly alerts with capability prefix",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.Finout,
                "read-alerts",
                "read anomaly alerts with capability prefix",
                RbacAccessType.Capability
            ),
            new(RbacNamespace.Azure, "create", "Create context/cloud resources", RbacAccessType.Capability),
            new(RbacNamespace.Azure, "read", "Read context/cloud resources", RbacAccessType.Capability),
            new(
                RbacNamespace.Azure,
                "read-provider",
                "Read resources in Azure resource group",
                RbacAccessType.Capability
            ),
            new(
                RbacNamespace.Azure,
                "manage-provider",
                "Manage resources in Azure resource group",
                RbacAccessType.Capability
            ),
            new(RbacNamespace.Rbac, "read", "Manage RBAC", RbacAccessType.Global),
            new(RbacNamespace.Rbac, "create", "Manage RBAC", RbacAccessType.Global),
            new(RbacNamespace.Rbac, "update", "Manage RBAC", RbacAccessType.Global),
            new(RbacNamespace.Rbac, "delete", "Manage RBAC", RbacAccessType.Global),
            new(
                RbacNamespace.ServiceCatalogue,
                "read",
                "Read service catalogue resources",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemLegacy,
                "read",
                "Read legacy system data (e.g. AAD-AWS sync capability list)",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "view-deleted-capabilities",
                "View deleted capabilities",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "unset-capability-tags",
                "Unset capability tags",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "create-demo-recording",
                "Create demo recordings",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "update-demo-recording",
                "Update demo recordings",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "delete-demo-recording",
                "Delete demo recordings",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "manage-permission-matrix",
                "Manage permission matrix",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "synchronize-aws-ecr-and-database-ecr",
                "Synchronize AWS ECR and database ECR",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "bypass-membership-approvals",
                "Bypass membership approvals",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "manage-self-assessment-options",
                "Manage self-assessment options",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "create-release-notes",
                "Create release notes",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "update-release-note",
                "Update release note",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "toggle-release-note-is-active",
                "Toggle release note active state",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "list-draft-release-notes",
                "List draft release notes",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "remove-release-note",
                "Remove release note",
                RbacAccessType.Global
            ),
            new(RbacNamespace.SystemAdmin, "create-event", "Create events", RbacAccessType.Global),
            new(RbacNamespace.SystemAdmin, "update-event", "Update events", RbacAccessType.Global),
            new(RbacNamespace.SystemAdmin, "delete-event", "Delete events", RbacAccessType.Global),
            new(RbacNamespace.SystemAdmin, "create-news-item", "Create news items", RbacAccessType.Global),
            new(RbacNamespace.SystemAdmin, "update-news-item", "Update news items", RbacAccessType.Global),
            new(RbacNamespace.SystemAdmin, "delete-news-item", "Delete news items", RbacAccessType.Global),
            new(RbacNamespace.SystemAdmin, "get-user-emails", "Get user emails", RbacAccessType.Global),
            new(
                RbacNamespace.CapabilityManagement,
                "batch-create-capabilities",
                "Create capabilities in batch as administrator",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "delete-membership-application-as-admin",
                "Delete membership applications as administrator",
                RbacAccessType.Global
            ),
            new(
                RbacNamespace.SystemAdmin,
                "retry-creating-message-contract",
                "Retry failed message contract creation as administrator",
                RbacAccessType.Global
            ),
        };

        return permissions;
    }

    public Permission(RbacNamespace ns, string name, string description, RbacAccessType RbacAccessType)
    {
        Name = name;
        Description = description;
        Namespace = ns;
        AccessType = RbacAccessType;
    }

    public Permission() { }

    public override string ToString()
    {
        return $"{Namespace} ({AccessType}) -- {Description}";
    }
}

public class PermissionMatrix
{
    public PermissionMatrix(Permission requestedPermission)
    {
        RequestedPermission = requestedPermission;
    }

    public Permission RequestedPermission { get; set; }
    public bool Permitted { get; set; } = false;
}

public class PermittedResponse
{
    public Dictionary<String, PermissionMatrix> PermissionMatrix { get; set; } = new();
    public List<RbacPermissionGrant> PermissionGrants { get; set; } = new();

    public bool Permitted()
    {
        return PermissionMatrix.All(kv => kv.Value.Permitted != false);
    }
}

public record RolePermissionEntry(RbacNamespace Namespace, string PermissionName, RbacAccessType AccessType);
