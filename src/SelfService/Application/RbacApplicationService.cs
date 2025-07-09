using SelfService.Domain.Models;

namespace SelfService.Application;

public class RbacApplicationService : IRbacApplicationService
{
    public Dictionary<String, Group> Groups;
    public List<AccessPolicy> AccessPolicies;
    private readonly IRbacPermissionGrantRepository _permissionGrantRepository;
    private readonly IRbacRoleGrantRepository _roleGrantRepository;

    public RbacApplicationService(IRbacPermissionGrantRepository permissionGrantRepository, IRbacRoleGrantRepository roleGrantRepository)
    {
        _permissionGrantRepository = permissionGrantRepository;
        _roleGrantRepository = roleGrantRepository;
        Groups = new Dictionary<String, Group>();
        AccessPolicies = new List<AccessPolicy>();
    }

    public async Task<PermittedResponse> IsUserPermitted(string user, List<Permission> permissions, string objectId)
    {
        var resp = new PermittedResponse();
        var userPolicies = GetApplicablePoliciesUser(user);
        permissions.ForEach(p => resp.PermissionMatrix.Add($"{p.Namespace}-{p.Name}", new PermissionMatrix(p)));
        
        // new
        var userPermissions = await GetPermissionGrantsForUser(user);
        var userRoles = await GetRoleGrantsForUser(user);
        
        
        var accessGrantingPermissionGrants = userPermissions.FindAll(p =>
        {
            var policyGrantsAccess = false;
            if (!p.Resource.Equals(objectId))
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
        
        
        // TODO: Decide on how a role grant actually maps to permissions
        // var accessGrantingRoleGrants = userRoles.FindAll(p =>
        // {
        //     var policyGrantsAccess = false;
        //     if (!p.Resource.Equals(objectId))
        //     {
        //         return false;
        //     }
        //     
        //     
        //     permissions.ForEach(pm =>
        //     {
        //         if (pm.Namespace == p.Namespace && pm.Name == p.Permission)
        //         {
        //             resp.PermissionMatrix[$"{p.Namespace}-{p.Permission}"].Permitted = true;
        //             policyGrantsAccess = true;
        //         }
        //     });
        //
        //     return policyGrantsAccess;
        // });
        
        return resp;
    }


    public async Task<List<RbacPermissionGrant>> GetPermissionGrantsForUser(string user)
    {
        
        return await _permissionGrantRepository.GetAllWithPredicate(p => p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user);
    }

    public async Task<List<RbacRoleGrant>> GetRoleGrantsForUser(string user)
    {
        return await _roleGrantRepository.GetAllWithPredicate(p => p.AssignedEntityType == AssignedEntityType.User && p.AssignedEntityId == user);
    }
    
    // old
    public List<AccessPolicy> GetApplicablePoliciesUser(string user)
    {
        var payload = new List<AccessPolicy>();
        foreach (var policy in AccessPolicies)
        {
            var isValid = false;
            foreach (var entity in policy.Entities)
            {
                switch (entity.EntityType)
                {
                    case EntityType.Group:
                        if (Groups.ContainsKey(entity.Id) && Groups[entity.Id].ContainsMember(user))
                        {
                            isValid = true;
                        }
                        break;
                    case EntityType.User:
                        if (entity.Id == user)
                        {
                            isValid = true;
                        }
                        break;
                    default:
                        isValid = false;
                        break;
                }
            }

            if (isValid)
            {
                payload.Add(policy);
            }
        }

        return payload;
    }
    
}

public enum AccessType
{
    Capability,
    Global,
    Aws,
    Azure
}

public class Group
{
    public List<String> Members { get; set; } = new List<String>();
    public String Name { get; set; } = "";
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool ContainsMember(String member)
    {
        return Members.Contains(member);
    }

    public Group()
    {
    }
}

public class AccessPolicy
{
    public List<Entity> Entities { get; set; } = new List<Entity>();
    public AccessType AccessType { get; set; } = AccessType.Capability;
    public List<String> ObjectIds { get; set; } = new List<string>();
    public List<Access> Accesses { get; set; } = new List<Access>();

    public AccessPolicy()
    {
        
    }
}

public class Entity
{
    public String Id { get; set; } = "";
    public EntityType EntityType { get; set; } = EntityType.Group;

    public Entity()
    {
        
    }
}

public enum Scope
{
    Create,Read,Update,Delete
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
            new Permission("topics", "create", "Create new topics", AccessType.Capability),
            new Permission("topics", "read-private", "Read private topics", AccessType.Capability),
            new Permission("topics", "read-public", "Read public topics", AccessType.Capability),
            new Permission("topics", "update", "Update topics", AccessType.Capability),
            new Permission("topics",  "delete", "Delete topics", AccessType.Capability),
            new Permission("capability-management", "receive-alerts", "Receive Alarms", AccessType.Capability),
            new Permission("capability-management", "receive-cost", "Receive cost summary reports", AccessType.Capability),
            new Permission("capability-management",  "request-deletion", "Request Capability deletion", AccessType.Capability),
            new Permission("capability-membership-management", "create", "Invite new member", AccessType.Capability),
            new Permission("capability-membership-management", "delete", "Remove member", AccessType.Capability),
            new Permission("capability-membership-management", "read", "See member list", AccessType.Capability),
            new Permission("capability-membership-management", "read-requests", "Read invitation/application requests", AccessType.Capability),
            new Permission("capability-membership-management", "manage-requests", "Approve/decline member requests", AccessType.Capability),
            new Permission("tags-and-metadata", "create", "Create", AccessType.Capability),
            new Permission("tags-and-metadata", "read", "Read", AccessType.Capability),
            new Permission("tags-and-metadata", "update", "Update", AccessType.Capability),
            new Permission("tags-and-metadata", "delete", "Delete", AccessType.Capability),
            new Permission("aws", "create", "Create context/cloud resources", AccessType.Capability),
            new Permission("aws", "read", "Read context/cloud resources", AccessType.Capability),
            new Permission("aws", "manage-provider", "Read resources in AWS account", AccessType.Capability),
            new Permission("aws", "read-provider", "Manage resources in AWS account", AccessType.Capability),
            new Permission("finout", "read-dashboards", "See all DFDS dashboards", AccessType.Global),
            new Permission("finout",  "manage-dashboards", "Manage dashboard with Capability prefix", AccessType.Capability),
            new Permission("finout", "manage-alerts", "Manage anomaly alerts with capability prefix", AccessType.Capability),
            new Permission("finout", "read-alerts", "read anomaly alerts with capability prefix", AccessType.Capability),
            new Permission("azure", "create", "Create context/cloud resources", AccessType.Capability),
            new Permission("azure", "read", "Read context/cloud resources", AccessType.Capability),
            new Permission("azure", "read-provider", "Read resources in Azure resource group", AccessType.Capability),
            new Permission("azure", "manage-provider", "Manage resources in Azure resource group", AccessType.Capability),
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

public class Access
{
    public List<Permission> Permissions { get; set; } = new List<Permission>();
}

public enum EntityType
{
    Group,
    User
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
    public List<AccessPolicy> AccessPolicies { get; set; } = new();

    public bool Permitted()
    {
        return PermissionMatrix.All(kv => kv.Value.Permitted != false);
    }
}