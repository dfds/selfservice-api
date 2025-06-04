namespace SelfService.Application;

public class RbacApplicationService : IRbacApplicationService
{
    private Dictionary<String, Group> _groups;
    private List<AccessPolicy> _accessPolicies;

    public RbacApplicationService()
    {
        _groups = new Dictionary<String, Group>();
        _accessPolicies = new List<AccessPolicy>();
    }

    public PermittedResponse IsUserPermitted(string user, List<Permission> permissions, string objectId)
    {
        var resp = new PermittedResponse();
        var userPolicies = GetApplicablePoliciesUser(user);
        permissions.ForEach(p => resp.PermissionMatrix.Add(p.Name, new PermissionMatrix(p)));
        
        var accessGrantingPolicies = userPolicies.FindAll(ap =>
        {
            var policyGrantsAccess = false;
            if (!ap.ObjectIds.Contains(objectId)) // If no match in policy for object, skip policy
            {
                return false;
            }
            
            ap.Accesses.ForEach(access =>
            {
                permissions.ForEach(p =>
                {
                    if (access.Permissions.Find(pm => pm.Namespace == p.Namespace && pm.Name == p.Name) != null)
                    {
                        resp.PermissionMatrix[p.Name].Permitted = true;
                        policyGrantsAccess = true;
                    }
                });

            });

            return policyGrantsAccess;
        });
        resp.AccessPolicies = accessGrantingPolicies;
        
        return resp;
    }
    
    public List<AccessPolicy> GetApplicablePoliciesUser(string user)
    {
        var payload = new List<AccessPolicy>();
        foreach (var policy in _accessPolicies)
        {
            var isValid = false;
            foreach (var entity in policy.Entities)
            {
                switch (entity.EntityType)
                {
                    case EntityType.Group:
                        if (_groups[entity.Id].ContainsMember(user))
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
    
    public static RbacApplicationService BootstrapTestService()
    {
        // groups
        var groups = new Dictionary<string, Group>();
        var groupCloudEngineeringCloudAdmin = new Group
        {
            Id = Guid.Parse("1C053AEC-FB32-4B32-8E0B-3FFE97C2C4D2"),
            Members = ["emcla@dfds.com"],
            Name = "CloudEngineering - CloudAdmin"
        };
        groups.Add(groupCloudEngineeringCloudAdmin.Id.ToString(), groupCloudEngineeringCloudAdmin);

        var permissions = Permission.BootstrapPermissions();
        
        // access policies
        var accessPolicies = new List<AccessPolicy>();
        accessPolicies.Add(new AccessPolicy
        {
            AccessType = AccessType.Capability,
            Entities =
            [
                new()
                {
                    EntityType = EntityType.Group,
                    Id = groupCloudEngineeringCloudAdmin.Id.ToString()
                },
                new ()
                {
                    EntityType = EntityType.User,
                    Id = "andfris@dfds.com"
                }
            ],
            ObjectIds = ["sandbox-emcla-pmyxn"],
            Accesses = [
                new()
                {
                    Target = "topics",
                    Permissions = [permissions.Find(x => x.Namespace == "topics" && x.Name == "read-private")!, permissions.Find(x => x.Namespace == "topics" && x.Name == "create")!]
                }
            ]
        });
        
        RbacApplicationService service = new RbacApplicationService
        {
            _groups = groups,
            _accessPolicies = accessPolicies
        };
        return service;
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
    public string Target { get; set; } = "";
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