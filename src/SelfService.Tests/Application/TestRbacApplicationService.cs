using SelfService.Application;

namespace SelfService.Tests.Application;


public class RbacTestData
{
    public static Group CloudEngineeringCloudAdmin { get; set; } = new Group {
        Id = Guid.Parse("1C053AEC-FB32-4B32-8E0B-3FFE97C2C4D2"),
        Members = ["emcla@dfds.com"],
        Name = "CloudEngineering - CloudAdmin"
    };
}

public class TestRbacApplicationService
{
    public static RbacApplicationService CreateTestRbacApplicationService(List<Group>? groupsSeed, List<AccessPolicy>? accessPoliciesSeed)
    {
        // groups
        Dictionary<string, Group> groups = new Dictionary<string, Group>();
        if (groupsSeed != null)
        {
            groupsSeed.ForEach(group => groups.Add(group.Id.ToString(), group));
        }
        else
        {
            groups.Add(RbacTestData.CloudEngineeringCloudAdmin.Id.ToString(), RbacTestData.CloudEngineeringCloudAdmin);
        }

        var permissions = Permission.BootstrapPermissions();
        
        // access policies
        List<AccessPolicy> accessPolicies = new List<AccessPolicy>();
        if (accessPoliciesSeed != null)
        {
            accessPolicies.AddRange(accessPoliciesSeed);
        }
        else
        {
            accessPolicies.Add(new AccessPolicy
            {
                AccessType = AccessType.Capability,
                Entities =
                [
                    new()
                    {
                        EntityType = EntityType.Group,
                        Id = RbacTestData.CloudEngineeringCloudAdmin.Id.ToString()
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
        }
        
        RbacApplicationService service = new RbacApplicationService
        {
            Groups = groups,
            AccessPolicies = accessPolicies
        };
        return service;
    }

    [Fact]
    public Task Baseline()
    {
        var rbacSvc = CreateTestRbacApplicationService(null, null);

        var cases = new List<PermittedResponse>
        {
            rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}, new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "read-public"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("andfris@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn"),
            rbacSvc.IsUserPermitted("andfris@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "andfris-sandbox-6-aeyex"),
        };
        
        cases.ForEach(c => Console.WriteLine(c.Permitted()));
        
        
        return Task.CompletedTask;
    }

    [Fact]
    public void UserWithTopicCreateCanCreate()
    {
        var rbacSvc = CreateTestRbacApplicationService(null, null);
        Assert.True(rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}], "sandbox-emcla-pmyxn").Permitted());
    }
    
    [Fact]
    public void UserWithTopicReadCanNotCreate()
    {
        var rbacSvc = CreateTestRbacApplicationService(null, null);
        Assert.False(rbacSvc.IsUserPermitted("emclaa@dfds.com", [new Permission { Namespace = "topics", Name = "create"}], "sandbox-emcla-pmyxn").Permitted());
    }
    
    [Fact]
    public void UserWithTopicReadPrivateCanReadPrivate()
    {
        var rbacSvc = CreateTestRbacApplicationService(null, null);
        Assert.True(rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn").Permitted());
    }
}