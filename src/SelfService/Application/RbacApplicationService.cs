using SelfService.Configuration;
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
            p.Type == RbacAccessType.Capability && p.Resource == capabilityId
        );
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForCapability(string capabilityId)
    {
        return await _roleGrantRepository.GetAllWithPredicate(p =>
            p.Type == RbacAccessType.Capability && p.Resource == capabilityId
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
        switch (permissionGrant.Type)
        {
            case var a when a == RbacAccessType.Global:
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
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
            case var a when a == RbacAccessType.Capability:
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
    }

    [TransactionalBoundary]
    public async Task GrantRoleGrant(string user, RbacRoleGrant roleGrant)
    {
        PermittedResponse? canUserCreateGlobalRbac;
        switch (roleGrant.Type)
        {
            case var a when a == RbacAccessType.Global:
                canUserCreateGlobalRbac = await IsUserPermitted(
                    user,
                    new List<Permission> { new(RbacNamespace.Rbac, "create", "", RbacAccessType.Global) },
                    roleGrant.Resource ?? ""
                );
                if (!canUserCreateGlobalRbac.Permitted())
                {
                    throw new UnauthorizedAccessException();
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
            case var a when a == RbacAccessType.Capability:
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
                var userGrantsToSelf =
                    user == roleGrant.AssignedEntityId && roleGrant.AssignedEntityType == AssignedEntityType.User;

                if (
                    (!canUserCreateGlobalRbac.Permitted() && !canUserCreateCapabilityRbac.Permitted())
                    || userGrantsToSelf
                )
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
    }

    [TransactionalBoundary]
    public async Task RevokeRoleGrant(string user, string id)
    {
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
            new(RbacNamespace.CapabilityManagement, "receive-cost", "Receive cost summary reports", RbacAccessType.Capability),
            new(RbacNamespace.CapabilityManagement, "request-deletion", "Request Capability deletion", RbacAccessType.Capability),
            new(RbacNamespace.CapabilityManagement, "manage-permissions", "Manage Capability permissions", RbacAccessType.Capability),
            new(RbacNamespace.CapabilityManagement, "read-self-assess", "Self assessment permissions", RbacAccessType.Capability),
            new(RbacNamespace.CapabilityManagement, "create-self-assess", "Self assessment permissions", RbacAccessType.Capability),
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
            new(RbacNamespace.Finout, "manage-dashboards", "Manage dashboard with Capability prefix", RbacAccessType.Capability),
            new(RbacNamespace.Finout, "manage-alerts", "Manage anomaly alerts with capability prefix", RbacAccessType.Capability),
            new(RbacNamespace.Finout, "read-alerts", "read anomaly alerts with capability prefix", RbacAccessType.Capability),
            new(RbacNamespace.Azure, "create", "Create context/cloud resources", RbacAccessType.Capability),
            new(RbacNamespace.Azure, "read", "Read context/cloud resources", RbacAccessType.Capability),
            new(RbacNamespace.Azure, "read-provider", "Read resources in Azure resource group", RbacAccessType.Capability),
            new(RbacNamespace.Azure, "manage-provider", "Manage resources in Azure resource group", RbacAccessType.Capability),
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
