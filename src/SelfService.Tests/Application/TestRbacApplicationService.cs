using SelfService.Application;
using Microsoft.Extensions.DependencyInjection;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Infrastructure.Api;

namespace SelfService.Tests.Application;


public class RbacTestData
{
    public static Group CloudEngineeringCloudAdmin { get; set; } = new Group {
        Id = Guid.Parse("1C053AEC-FB32-4B32-8E0B-3FFE97C2C4D2"),
        Members = ["emcla@dfds.com"],
        Name = "CloudEngineering - CloudAdmin"
    };

    public static async Task<RbacInMemoryTestFixture> NewInMemoryFixture(bool populateDatabase = true, List<RbacPermissionGrant>? rbacPermissionGrantsSeed = null, List<RbacRoleGrant>? rbacRoleGrantsSeed = null)
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        if (populateDatabase)
        {
            TestRbacApplicationService.PopulateRbac(dbContext, rbacPermissionGrantsSeed, rbacRoleGrantsSeed);
        }
        
        TestRbacApplicationService.CreateTestRbacApplicationService(dbContext, null, null);
        var application = await new ApiApplicationBuilder()
            .WithSelfServiceDbContext(dbContext)
            .ConfigureRbac()
            .BuildAsync();

        var fixture = new RbacInMemoryTestFixture(databaseFactory, dbContext, application);
        return fixture;
    }
}

public class RbacInMemoryTestFixture
{
    public InMemoryDatabaseFactory InMemoryDatabaseFactory { get; set; }
    public ApiApplication ApiApplication { get; set; }
    
    public SelfServiceDbContext DbContext { get; set; }

    public RbacInMemoryTestFixture(InMemoryDatabaseFactory inMemoryDatabaseFactory, SelfServiceDbContext selfServiceDbContext, ApiApplication apiApplication)
    {
        InMemoryDatabaseFactory = inMemoryDatabaseFactory;
        DbContext = selfServiceDbContext;
        ApiApplication = apiApplication;
    }
}

public class TestRbacApplicationService
{
    internal static void CreateTestRbacApplicationService(SelfServiceDbContext dbContext, List<Group>? groupsSeed, List<AccessPolicy>? accessPoliciesSeed)
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
                        Permissions = [permissions.Find(x => x.Namespace == "topics" && x.Name == "read-private")!, permissions.Find(x => x.Namespace == "topics" && x.Name == "create")!]
                    }
                ]
            });   
        }
    }

    internal async static void PopulateRbac(SelfServiceDbContext dbContext, List<RbacPermissionGrant>? rbacPermissionGrantsSeed, List<RbacRoleGrant>? rbacRoleGrantsSeed)
    {
        if (rbacPermissionGrantsSeed != null)
        {
            dbContext.RbacPermissionGrants.AddRange(rbacPermissionGrantsSeed);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            dbContext.RbacPermissionGrants.Add(new RbacPermissionGrant (
                id: RbacPermissionGrantId.New(),
                createdAt: DateTime.Now,
                assignedEntityType: AssignedEntityType.User,
                assignedEntityId: "andfris@dfds.com",
                @namespace: "topics",
                permission: "create",
                type: "capability",
                resource: "sandbox-emcla-pmyxn"
                ));
            
            await dbContext.SaveChangesAsync();
        }

        if (rbacRoleGrantsSeed != null)
        {
            dbContext.RbacRoleGrants.AddRange(rbacRoleGrantsSeed);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            dbContext.RbacRoleGrants.Add(new RbacRoleGrant (
                id: RbacRoleGrantId.New(),
                createdAt: DateTime.Now,
                assignedEntityType: AssignedEntityType.User,
                assignedEntityId: "andfris@dfds.com",
                name: "Contributor",
                type: "capability",
                resource: "sandbox-emcla-pmyxn"
            ));
            
            await dbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async void Baseline()
    {
        var fixture = await RbacTestData.NewInMemoryFixture();
        var rbacSvc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;
        
        var cases = new List<PermittedResponse>
        {
            await rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}], "sandbox-emcla-pmyxn"),
            await rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}, new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn"),
            await rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "read-public"}], "sandbox-emcla-pmyxn"),
            await rbacSvc.IsUserPermitted("andfris@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn"),
            await rbacSvc.IsUserPermitted("andfris@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "andfris-sandbox-6-aeyex"),
        };
        
        cases.ForEach(c => Console.WriteLine(c.Permitted()));
    }

    [Fact]
    public async void UserAllow()
    {
        var test01GroupId = Guid.NewGuid();
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        CreateTestRbacApplicationService(dbContext, [
            new Group
            {
                Id = test01GroupId,
                Name = "test01 - users",
                Members = ["test01@dfds.cloud"]
            }
        ], [
            new AccessPolicy {
                AccessType = AccessType.Capability,
                ObjectIds = ["test01"],
                Entities = [new Entity {EntityType = EntityType.Group, Id = test01GroupId.ToString()}],
                Accesses = [new Access {Permissions = [
                    new Permission { Namespace = "topics", Name = "create"},
                    new Permission { Namespace = "topics", Name = "read-private"}
                ]}]
            },
            new AccessPolicy {
                AccessType = AccessType.Capability,
                ObjectIds = ["test02"],
                Entities = [new Entity {EntityType = EntityType.Group, Id = test01GroupId.ToString()}, new Entity {EntityType = EntityType.User, Id = "test03@dfds.cloud"}],
                Accesses = [new Access {Permissions = [
                    new Permission { Namespace = "topics", Name = "read-public"}
                ]}]
            }
        ]);
        var application = await new ApiApplicationBuilder()
            .WithSelfServiceDbContext(dbContext)
            .ConfigureRbac()
            .BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;

        Assert.True((await rbacSvc.IsUserPermitted("test01@dfds.cloud", [new Permission { Namespace = "topics", Name = "create"}], "test01")).Permitted());
        Assert.True((await rbacSvc.IsUserPermitted("test01@dfds.cloud", [new Permission { Namespace = "topics", Name = "read-private"}], "test01")).Permitted());
        Assert.True((await rbacSvc.IsUserPermitted("test01@dfds.cloud", [new Permission { Namespace = "topics", Name = "read-public"}], "test02")).Permitted());
        Assert.True((await rbacSvc.IsUserPermitted("test03@dfds.cloud", [new Permission { Namespace = "topics", Name = "read-public"}], "test02")).Permitted());
    }
    
    [Fact]
    public async void UserDeny()
    {
        var test01GroupId = Guid.NewGuid();
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        CreateTestRbacApplicationService(dbContext, [
            new Group
            {
                Id = test01GroupId,
                Name = "test01 - users",
                Members = ["test01@dfds.cloud"]
            }
        ], [
            new AccessPolicy {
                AccessType = AccessType.Capability,
                ObjectIds = ["test01"],
                Entities = [new Entity {EntityType = EntityType.Group, Id = test01GroupId.ToString()}],
                Accesses = [new Access {Permissions = [
                    new Permission { Namespace = "topics", Name = "create"},
                    new Permission { Namespace = "topics", Name = "read-private"}
                ]}]
            },
            new AccessPolicy {
                AccessType = AccessType.Capability,
                ObjectIds = ["test02"],
                Entities = [new Entity {EntityType = EntityType.Group, Id = test01GroupId.ToString()}],
                Accesses = [new Access {Permissions = [
                    new Permission { Namespace = "topics", Name = "read-public"}
                ]}]
            }
        ]);
        var application = await new ApiApplicationBuilder()
            .WithSelfServiceDbContext(dbContext)
            .ConfigureRbac()
            .BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        
        Assert.False((await rbacSvc.IsUserPermitted("test02@dfds.cloud", [new Permission { Namespace = "topics", Name = "create"}], "test01")).Permitted());
        Assert.False((await rbacSvc.IsUserPermitted("test02@dfds.cloud", [new Permission { Namespace = "topics", Name = "read-private"}], "test01")).Permitted());
        Assert.False((await rbacSvc.IsUserPermitted("test02@dfds.cloud", [new Permission { Namespace = "topics", Name = "read-public"}], "test02")).Permitted());
        Assert.False((await rbacSvc.IsUserPermitted("test01@dfds.cloud", [new Permission { Namespace = "topics", Name = "read-public"}], "test01")).Permitted());
        Assert.False((await rbacSvc.IsUserPermitted("test01@dfds.cloud", [new Permission { Namespace = "capability-management", Name = "request-deletion"}], "test01")).Permitted());
    }

    [Fact]
    public async void UserWithTopicCreateCanCreate()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        CreateTestRbacApplicationService(dbContext, null, null);
        var application = await new ApiApplicationBuilder()
            .WithSelfServiceDbContext(dbContext)
            .ConfigureRbac()
            .BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        
        Assert.True((await rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "create"}], "sandbox-emcla-pmyxn")).Permitted());
    }
    
    [Fact]
    public async void UserWithTopicReadCanNotCreate()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        CreateTestRbacApplicationService(dbContext, null, null);
        var application = await new ApiApplicationBuilder()
            .WithSelfServiceDbContext(dbContext)
            .ConfigureRbac()
            .BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        
        Assert.False((await rbacSvc.IsUserPermitted("emclaa@dfds.com", [new Permission { Namespace = "topics", Name = "create"}], "sandbox-emcla-pmyxn")).Permitted());
    }
    
    [Fact]
    public async void UserWithTopicReadPrivateCanReadPrivate()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        CreateTestRbacApplicationService(dbContext, null, null);
        var application = await new ApiApplicationBuilder()
            .WithSelfServiceDbContext(dbContext)
            .ConfigureRbac()
            .BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        
        Assert.True((await rbacSvc.IsUserPermitted("emcla@dfds.com", [new Permission { Namespace = "topics", Name = "read-private"}], "sandbox-emcla-pmyxn")).Permitted());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void UserAllowUsingDbStore()
    {
        var application = await new ApiApplicationBuilder()
            .WithLocalDb()
            .ConfigureRbac()
            .BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        var dbContext = application.Services.GetService<SelfServiceDbContext>()!;

        var permissions = dbContext.RbacPermissionGrants.ToList();
        var roles = dbContext.RbacRoleGrants.ToList();
    }
}