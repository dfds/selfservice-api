using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class RbacApplicationService : IRbacApplicationService
{
    private readonly IRbacPermissionGrantRepository _permissionGrantRepository;
    private readonly IRbacRoleGrantRepository _roleGrantRepository;
    private readonly IRbacGroupRepository _groupRepository;
    private readonly IPermissionQuery _permissionQuery;
    private readonly IRbacRoleRepository _roleRepository;

    public RbacApplicationService(
        IRbacPermissionGrantRepository permissionGrantRepository,
        IRbacRoleGrantRepository roleGrantRepository,
        IRbacGroupRepository groupRepository,
        IPermissionQuery permissionQuery,
        IRbacRoleRepository roleRepository
    )
    {
        _permissionGrantRepository = permissionGrantRepository;
        _roleGrantRepository = roleGrantRepository;
        _groupRepository = groupRepository;
        _permissionQuery = permissionQuery;
        _roleRepository = roleRepository;
    }

    public async Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId)
    {
        var resp = new PermittedResponse();
        permissions.ForEach(p => resp.PermissionMatrix.Add($"{p.Namespace}-{p.Name}", new PermissionMatrix(p)));

        // user level
        var userPermissions = await GetPermissionGrantsForUser(user);
        var userRoles = await GetRoleGrantsForUser(user);

        // group level
        var groupPermissions = await _permissionQuery.FindUserGroupPermissionsByUserId(user);
        var groupRoles = await _permissionQuery.FindUserGroupRolesByUserId(user);

        var combinedPermissions = userPermissions.Concat(groupPermissions).ToList();
        var combinedRoles = userRoles.Concat(groupRoles).ToList();

        var accessGrantingPermissionGrants = combinedPermissions.FindAll(p =>
        {
            var policyGrantsAccess = false;
            if (!p.Type.Equals("Global") && !p.Resource!.Equals(objectId))
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
            if (!p.Type.Equals("Global") && !p.Resource!.Equals(objectId))
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
        return await _permissionGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user
        );
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user)
    {
        return await _roleGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user
        );
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForGroup(string groupId)
    {
        return await _permissionGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.Group && p.AssignedEntityId == groupId
        );
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForCapability(string capabilityId)
    {
        return await _permissionGrantRepository.GetAllWithPredicate(p =>
            p.Type.ToLower() == "capability" && p.Resource == capabilityId
        );
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForCapability(string capabilityId)
    {
        return await _roleGrantRepository.GetAllWithPredicate(p =>
            p.Type.ToLower() == "capability" && p.Resource == capabilityId
        );
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForGroup(string groupId)
    {
        return await _roleGrantRepository.GetAllWithPredicate(p =>
            p.AssignedEntityType == AssignedEntityType.Group && p.AssignedEntityId == groupId
        );
    }

    public async Task<List<RbacGroup>> GetGroupsForUser(string user)
    {
        var groups = await _groupRepository.GetAllWithPredicate(x => x.Members.Any(m => m.UserId == user));

        return groups;
    }

    // TODO: Implement when repo is available
    public async Task<List<RbacRole>> GetAssignableRoles()
    {
        var roles = await _roleRepository.GetAll();
        return roles;
    }

    [TransactionalBoundary]
    public async Task GrantPermission(string user, RbacPermissionGrant permissionGrant)
    {
        PermittedResponse? canUserCreateGlobalRbac;
        switch (permissionGrant.Type.ToLower())
        {
            case "global":
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "create", "", AccessType.Global) },
                    permissionGrant.Resource ?? ""
                );
                if (!canUserCreateGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
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
            case "capability":
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "create", "", AccessType.Global) },
                    permissionGrant.Resource ?? ""
                );
                var canUserCreateCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new("capability-management", "manage-permissions", "", AccessType.Capability),
                    },
                    permissionGrant.Resource ?? ""
                );

                if (!canUserCreateGlobalRbac.Permitted() && !canUserCreateCapabilityRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
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
    }

    [TransactionalBoundary]
    public async Task RevokePermission(string user, string id)
    {
        var permissionLookup = await _permissionGrantRepository.FindById(RbacPermissionGrantId.Parse(id));
        if (permissionLookup == null)
            throw new Exception("Permission grant not found");

        PermittedResponse? canUserCreateGlobalRbac;
        switch (permissionLookup.Type.ToLower())
        {
            case "global":
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "delete", "", AccessType.Global) },
                    permissionLookup.Resource ?? ""
                );
                if (!canUserCreateGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                await _permissionGrantRepository.Remove(permissionLookup.Id);
                break;
            case "capability":
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "delete", "", AccessType.Global) },
                    permissionLookup.Resource ?? ""
                );
                var canUserCreateCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new("capability-management", "manage-permissions", "", AccessType.Capability),
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
    }

    [TransactionalBoundary]
    public async Task GrantRoleGrant(string user, RbacRoleGrant roleGrant)
    {
        PermittedResponse? canUserCreateGlobalRbac;
        switch (roleGrant.Type.ToLower())
        {
            case "global":
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "create", "", AccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                if (!canUserCreateGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                var existingGlobalGrant = await _roleGrantRepository.FindByPredicate(rg =>
                    rg.AssignedEntityId == roleGrant.AssignedEntityId && rg.Type.ToLower() == roleGrant.Type.ToLower()
                );
                if (existingGlobalGrant != null)
                {
                    await _roleGrantRepository.Remove(existingGlobalGrant.Id);
                }
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
            case "capability":
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "create", "", AccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                var canUserCreateCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new("capability-management", "manage-permissions", "", AccessType.Capability),
                    },
                    roleGrant.Resource ?? ""
                );

                if (!canUserCreateGlobalRbac.Permitted() && !canUserCreateCapabilityRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }

                if (roleGrant.Resource == null)
                {
                    throw new BadHttpRequestException("Capability ID is required for capability role grants");
                }

                var existingCapabilityGrant = await _roleGrantRepository.FindByPredicate(rg =>
                    rg.AssignedEntityId == roleGrant.AssignedEntityId
                    && rg.Resource == roleGrant.Resource
                    && rg.Type.ToLower() == roleGrant.Type.ToLower()
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
    }

    [TransactionalBoundary]
    public async Task RevokeRoleGrant(string user, string id)
    {
        var roleGrant = await _roleGrantRepository.FindById(RbacRoleGrantId.Parse(id));
        if (roleGrant == null)
            throw new Exception("Role grant not found");

        PermittedResponse? canUserDeleteGlobalRbac;
        switch (roleGrant.Type.ToLower())
        {
            case "global":
                canUserDeleteGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "delete", "", AccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                if (!canUserDeleteGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
                }
                await _roleGrantRepository.Remove(roleGrant.Id);
                break;
            case "capability":
                canUserDeleteGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new("rbac", "delete", "", AccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                var canUserDeleteCapabilityRbac = await IsUserPermitted(
                    user,
                    new List<Permission>
                    {
                        new("capability-management", "manage-permissions", "", AccessType.Capability),
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
    }

    public async Task<bool> CanModifyCapabilityRbac(string user, string id)
    {
        var resp = await IsUserPermitted(
            user,
            new List<Permission>
            {
                new()
                {
                    Namespace = "capability-management",
                    Name = "manage-permissions",
                    AccessType = AccessType.Global,
                },
                new()
                {
                    Namespace = "rbac",
                    Name = "create",
                    AccessType = AccessType.Global,
                },
                new()
                {
                    Namespace = "rbac",
                    Name = "update",
                    AccessType = AccessType.Global,
                },
                new()
                {
                    Namespace = "rbac",
                    Name = "delete",
                    AccessType = AccessType.Global,
                },
            },
            id
        );

        return resp.PermissionMatrix.Any(x => x.Value.Permitted);
    }
}

public enum AccessType
{
    Capability,
    Global,
    Aws,
    Azure,
}

public class Permission
{
    public String Description { get; set; } = "";
    public String Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public AccessType AccessType { get; set; } = AccessType.Capability;

    public static List<Permission> BootstrapPermissions()
    {
        var permissions = new List<Permission>
        {
            new("topics", "create", "Create new topics", AccessType.Capability),
            new("topics", "read-private", "Read private topics", AccessType.Capability),
            new("topics", "read-public", "Read public topics", AccessType.Capability),
            new("topics", "update", "Update topics", AccessType.Capability),
            new("topics", "delete", "Delete topics", AccessType.Capability),
            new("capability-management", "receive-alerts", "Receive Alarms", AccessType.Capability),
            new("capability-management", "receive-cost", "Receive cost summary reports", AccessType.Capability),
            new("capability-management", "request-deletion", "Request Capability deletion", AccessType.Capability),
            new("capability-management", "manage-permissions", "Manage Capability permissions", AccessType.Capability),
            new("capability-management", "read-self-assess", "Self assessment permissions", AccessType.Capability),
            new("capability-management", "create-self-assess", "Self assessment permissions", AccessType.Capability),
            new("capability-membership-management", "create", "Invite new member", AccessType.Capability),
            new("capability-membership-management", "delete", "Remove member", AccessType.Capability),
            new("capability-membership-management", "read", "See member list", AccessType.Capability),
            new(
                "capability-membership-management",
                "read-requests",
                "Read invitation/application requests",
                AccessType.Capability
            ),
            new(
                "capability-membership-management",
                "manage-requests",
                "Approve/decline member requests",
                AccessType.Capability
            ),
            new("tags-and-metadata", "create", "Create", AccessType.Capability),
            new("tags-and-metadata", "read", "Read", AccessType.Capability),
            new("tags-and-metadata", "update", "Update", AccessType.Capability),
            new("tags-and-metadata", "delete", "Delete", AccessType.Capability),
            new("aws", "create", "Create context/cloud resources", AccessType.Capability),
            new("aws", "read", "Read context/cloud resources", AccessType.Capability),
            new("aws", "manage-provider", "Read resources in AWS account", AccessType.Capability),
            new("aws", "read-provider", "Manage resources in AWS account", AccessType.Capability),
            new("finout", "read-dashboards", "See all DFDS dashboards", AccessType.Global),
            new("finout", "manage-dashboards", "Manage dashboard with Capability prefix", AccessType.Capability),
            new("finout", "manage-alerts", "Manage anomaly alerts with capability prefix", AccessType.Capability),
            new("finout", "read-alerts", "read anomaly alerts with capability prefix", AccessType.Capability),
            new("azure", "create", "Create context/cloud resources", AccessType.Capability),
            new("azure", "read", "Read context/cloud resources", AccessType.Capability),
            new("azure", "read-provider", "Read resources in Azure resource group", AccessType.Capability),
            new("azure", "manage-provider", "Manage resources in Azure resource group", AccessType.Capability),
            new("rbac", "read", "Manage RBAC", AccessType.Global),
            new("rbac", "create", "Manage RBAC", AccessType.Global),
            new("rbac", "update", "Manage RBAC", AccessType.Global),
            new("rbac", "delete", "Manage RBAC", AccessType.Global),
        };

        return permissions;
    }

    public Permission(string ns, string name, string description, AccessType accessType)
    {
        Name = name;
        Description = description;
        Namespace = ns;
        AccessType = accessType;
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
