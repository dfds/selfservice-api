using Microsoft.Extensions.Caching.Memory;
using SelfService.Configuration;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

class CacheConst
{
    public const string RbacPermissionGrantCacheKey = "RbacPermissionGrantCacheKey";
    public const string PermissionGrantsForUser = "PermissionGrantsForUser";
    public const string RoleGrantsForUser = "RoleGrantsForUser";
    public const string UserGroupPermissions = "UserGroupPermissions";
    public const string UserGroupRoles = "UserGroupRoles";
    public const string PermissionGrantsForRole = "PermissionGrantsForRole";
    public const string PermissionGrantsForGroup = "PermissionGrantsForGroup";
    public const string PermissionGrantsForCapability = "PermissionGrantsForCapability";
    public const string RoleGrantsForCapability = "RoleGrantsForCapability";
    public const string RoleGrantsForGroup = "RoleGrantsForGroup";
    public const string GroupsForUser = "GroupsForUser";
    public const string AssignableRoles = "AssignableRoles";
    public const string SystemGroups = "SystemGroups";
    
}

class Cache
{
    private IMemoryCache _cache;
    
    public Cache()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Set(string prefix, string key, object value)
    {
        var formattedKey = $"{prefix}-{key}";
        _cache.Set(formattedKey, value);
    }

    public object? Get(string prefix, string key)
    {
        var formattedKey = $"{prefix}-{key}";
        _cache.TryGetValue(formattedKey, out var value);
        return value;
    }
    public T? Get<T>(string prefix, string key)
    {
        var formattedKey = $"{prefix}-{key}";
        _cache.TryGetValue(formattedKey, out var value);
        return (T?)value;
    }

    public void Reset()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
}

public class RbacApplicationService : IRbacApplicationService
{
    private readonly IRbacPermissionGrantRepository _permissionGrantRepository;
    private readonly IRbacRoleGrantRepository _roleGrantRepository;
    private readonly IRbacGroupMemberRepository _groupMemberRepository;
    private readonly IRbacGroupRepository _groupRepository;
    private readonly IPermissionQuery _permissionQuery;
    private readonly IRbacRoleRepository _roleRepository;
    private readonly Cache _cache;

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
        _cache = new Cache();
    }

    /*
        [Note 2025-09-18 by andfris]
        The permission checks in this service are commented out for now, as they interfere with bootstrapping
        and with the Cloud Engineer role which is supposed to have blanket permissions.
    */

    public async Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId)
    {
        var resp = new PermittedResponse();
        permissions.ForEach(p => resp.PermissionMatrix.Add($"{p.Namespace}-{p.Name}", new PermissionMatrix(p)));

        // user level
        var userPermissions = await GetPermissionGrantsForUser(user);
        var userRoles = await GetRoleGrantsForUser(user);

        // group level
        IList<RbacPermissionGrant> groupPermissions;
        var cacheGroupPermissions = _cache.Get<List<RbacPermissionGrant>>(CacheConst.UserGroupPermissions, user);
        if (cacheGroupPermissions == null)
        {
            groupPermissions = await _permissionQuery.FindUserGroupPermissionsByUserId(user);
            _cache.Set(CacheConst.UserGroupPermissions, user, groupPermissions);
        }
        else
        {
            groupPermissions = cacheGroupPermissions;
        }
        
        IList<RbacRoleGrant> groupRoles;
        var cacheGroupRoles = _cache.Get<List<RbacRoleGrant>>(CacheConst.UserGroupRoles, user);
        if (cacheGroupRoles == null)
        {
            groupRoles = await _permissionQuery.FindUserGroupRolesByUserId(user);
            _cache.Set(CacheConst.UserGroupRoles, user, groupRoles);
        }
        else
        {
            groupRoles = cacheGroupRoles;
        }

        var combinedPermissions = userPermissions.Concat(groupPermissions).ToList();
        var combinedRoles = userRoles.Concat(groupRoles).ToList();

        var accessGrantingPermissionGrants = combinedPermissions.FindAll(p =>
        {
            var policyGrantsAccess = false;
            if (p.Type != RbacAccessType.Global && !p.Resource!.Equals(objectId))
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
            if (p.Type != RbacAccessType.Global && !p.Resource!.Equals(objectId))
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
            var foundPermissions = await _permissionGrantRepository.GetAllWithPredicate(p =>
                p.AssignedEntityType == AssignedEntityType.Role
                && string.Equals(p.AssignedEntityId, rg.RoleId.ToString(), StringComparison.CurrentCultureIgnoreCase)
            );
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
        var cacheResp = _cache.Get<List<RbacPermissionGrant>>(CacheConst.PermissionGrantsForUser, user);
        if (cacheResp != null) return cacheResp;
        
        var resp = await _permissionGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user
        );
        _cache.Set(CacheConst.PermissionGrantsForUser, user, resp);
        return resp;
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user)
    {
        var cacheResp = _cache.Get<List<RbacRoleGrant>>(CacheConst.RoleGrantsForUser, user);
        if (cacheResp != null) return cacheResp;

        var resp =  await _roleGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user
        );
        _cache.Set(CacheConst.RoleGrantsForUser, user, resp);
        return resp;
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForGroup(string groupId)
    {
        var cacheResp = _cache.Get<List<RbacPermissionGrant>>(CacheConst.PermissionGrantsForGroup, groupId);
        if (cacheResp != null) return cacheResp;

        var resp =  await _permissionGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.Group && p.AssignedEntityId == groupId
        );
        _cache.Set(CacheConst.PermissionGrantsForGroup, groupId, resp);
        return resp;
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForRole(string roleId)
    {
        var cacheResp = _cache.Get<List<RbacPermissionGrant>>(CacheConst.PermissionGrantsForRole, roleId);
        if (cacheResp != null) return cacheResp;
        
        var resp = await _permissionGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.Role && p.AssignedEntityId == roleId
        );
        _cache.Set(CacheConst.PermissionGrantsForRole, roleId, resp);
        return resp;
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForCapability(string capabilityId)
    {
        var cacheResp = _cache.Get<List<RbacPermissionGrant>>(CacheConst.PermissionGrantsForCapability, capabilityId);
        if (cacheResp != null) return cacheResp;

        var resp = await _permissionGrantRepository.GetAllWithPredicate(p =>
            p.Type == RbacAccessType.Capability && p.Resource == capabilityId
        );
        _cache.Set(CacheConst.PermissionGrantsForCapability, capabilityId, resp);
        return resp;
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForCapability(string capabilityId)
    {
        var cacheResp = _cache.Get<List<RbacRoleGrant>>(CacheConst.RoleGrantsForCapability, capabilityId);
        if (cacheResp != null) return cacheResp;
        
        var resp =  await _roleGrantRepository.GetAllWithPredicate(p =>
            p.Type == RbacAccessType.Capability && p.Resource == capabilityId
        );
        _cache.Set(CacheConst.RoleGrantsForCapability, capabilityId, resp);
        return resp;
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForGroup(string groupId)
    {
        var cacheResp = _cache.Get<List<RbacRoleGrant>>(CacheConst.RoleGrantsForGroup, groupId);
        if (cacheResp != null) return cacheResp;
        
        var resp =  await _roleGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.Group && p.AssignedEntityId == groupId
        );
        _cache.Set(CacheConst.RoleGrantsForGroup, groupId, resp);
        return resp;
    }

    public async Task<List<RbacGroup>> GetGroupsForUser(string user)
    {
        var cacheResp = _cache.Get<List<RbacGroup>>(CacheConst.GroupsForUser, user);
        if (cacheResp != null) return cacheResp;
        
        var resp = await _groupRepository.GetAllWithPredicate(x => x.Members.Any(m => m.UserId == user));
        _cache.Set(CacheConst.GroupsForUser, user, resp);
        return resp;
    }

    // TODO: Implement when repo is available
    public async Task<List<RbacRole>> GetAssignableRoles()
    {
        var cacheResp = _cache.Get<List<RbacRole>>(CacheConst.AssignableRoles, "all");
        if (cacheResp != null) return cacheResp;
        
        var resp = await _roleRepository.GetAll();
        _cache.Set(CacheConst.AssignableRoles,"all", resp);
        return resp;
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
    public async Task RevokeRoleGrant(string user, string id)
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
        var cacheResp = _cache.Get<List<RbacGroup>>(CacheConst.SystemGroups, "all");
        if (cacheResp != null) return cacheResp;
        
        var resp = await _groupRepository.GetAll();
        _cache.Set(CacheConst.SystemGroups, "all",  resp);
        return resp;
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
        await _groupMemberRepository.Remove(membership.Id);
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
}

public class Permission
{
    public String Description { get; set; } = "";
    public String Name { get; set; } = "";
    public RbacNamespace Namespace { get; set; } = RbacNamespace.Default;
    public RbacAccessType AccessType { get; set; } = RbacAccessType.Capability;

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