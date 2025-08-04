using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Application;

public class RbacApplicationService : IRbacApplicationService
{
    private readonly IRbacPermissionGrantRepository _permissionGrantRepository;
    private readonly IRbacRoleGrantRepository _roleGrantRepository;
    private readonly IRbacGroupRepository _groupRepository;
    private readonly IPermissionQuery _permissionQuery;

    public RbacApplicationService(IRbacPermissionGrantRepository permissionGrantRepository, IRbacRoleGrantRepository roleGrantRepository, IRbacGroupRepository groupRepository, IPermissionQuery permissionQuery)
    {
        _permissionGrantRepository = permissionGrantRepository;
        _roleGrantRepository = roleGrantRepository;
        _groupRepository = groupRepository;
        _permissionQuery = permissionQuery;
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

        // weee
        
        return resp;
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForRoleGrants(List<RbacRoleGrant> roleGrants)
    {
        var payload = new List<RbacPermissionGrant>();
        foreach (var rg in roleGrants)
        {
            var foundPermissions = await _permissionGrantRepository.GetAllWithPredicate(p => p.AssignedEntityType == AssignedEntityType.Role && string.Equals(p.AssignedEntityId, rg.RoleId.ToString(), StringComparison.CurrentCultureIgnoreCase));
            
            // override type & resource from grant to permission
            foreach (var rbacPermissionGrant in foundPermissions)
            {
                rbacPermissionGrant.Type = rg.Type;
                rbacPermissionGrant.Resource = rg.Resource;
            }
            
            payload.AddRange(foundPermissions);
        }

        return payload;
    }

    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForUser(string user)
    {
        return await _permissionGrantRepository.GetAllWithPredicate(p => p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user);
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user)
    {
        return await _roleGrantRepository.GetAllWithPredicate(p => p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user);
    }
    
    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForGroup(string groupId)
    {
        return await _permissionGrantRepository.GetAllWithPredicate(p => p.AssignedEntityType == AssignedEntityType.Group && p.AssignedEntityId == groupId);
    }
}

public enum AccessType
{
    Capability,
    Global,
    Aws,
    Azure
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
            new ("topics", "create", "Create new topics", AccessType.Capability),
            new ("topics", "read-private", "Read private topics", AccessType.Capability),
            new ("topics", "read-public", "Read public topics", AccessType.Capability),
            new ("topics", "update", "Update topics", AccessType.Capability),
            new ("topics",  "delete", "Delete topics", AccessType.Capability),
            new ("capability-management", "receive-alerts", "Receive Alarms", AccessType.Capability),
            new ("capability-management", "receive-cost", "Receive cost summary reports", AccessType.Capability),
            new ("capability-management",  "request-deletion", "Request Capability deletion", AccessType.Capability),
            new ("capability-management",  "manage-permissions", "Manage Capability permissions", AccessType.Capability),
            new ("capability-management",  "read-self-assess", "Self assessment permissions", AccessType.Capability),
            new ("capability-management",  "create-self-assess", "Self assessment permissions", AccessType.Capability),
            new ("capability-membership-management", "create", "Invite new member", AccessType.Capability),
            new ("capability-membership-management", "delete", "Remove member", AccessType.Capability),
            new ("capability-membership-management", "read", "See member list", AccessType.Capability),
            new ("capability-membership-management", "read-requests", "Read invitation/application requests", AccessType.Capability),
            new ("capability-membership-management", "manage-requests", "Approve/decline member requests", AccessType.Capability),
            new ("tags-and-metadata", "create", "Create", AccessType.Capability),
            new ("tags-and-metadata", "read", "Read", AccessType.Capability),
            new ("tags-and-metadata", "update", "Update", AccessType.Capability),
            new ("tags-and-metadata", "delete", "Delete", AccessType.Capability),
            new ("aws", "create", "Create context/cloud resources", AccessType.Capability),
            new ("aws", "read", "Read context/cloud resources", AccessType.Capability),
            new ("aws", "manage-provider", "Read resources in AWS account", AccessType.Capability),
            new ("aws", "read-provider", "Manage resources in AWS account", AccessType.Capability),
            new ("finout", "read-dashboards", "See all DFDS dashboards", AccessType.Global),
            new ("finout",  "manage-dashboards", "Manage dashboard with Capability prefix", AccessType.Capability),
            new ("finout", "manage-alerts", "Manage anomaly alerts with capability prefix", AccessType.Capability),
            new ("finout", "read-alerts", "read anomaly alerts with capability prefix", AccessType.Capability),
            new ("azure", "create", "Create context/cloud resources", AccessType.Capability),
            new ("azure", "read", "Read context/cloud resources", AccessType.Capability),
            new ("azure", "read-provider", "Read resources in Azure resource group", AccessType.Capability),
            new ("azure", "manage-provider", "Manage resources in Azure resource group", AccessType.Capability),
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

    public Permission()
    {
        
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