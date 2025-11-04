using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Infrastructure.Api;

namespace SelfService.Tests.Application;

public class RbacTestData
{
    public static async Task<RbacInMemoryTestFixture> NewInMemoryFixture(
        bool populateDatabase = true,
        List<RbacPermissionGrant>? rbacPermissionGrantsSeed = null,
        List<RbacRoleGrant>? rbacRoleGrantsSeed = null,
        List<RbacGroup>? rbacGroupSeed = null
    )
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        if (populateDatabase)
        {
            TestRbacApplicationService.PopulateRbac(
                dbContext,
                rbacPermissionGrantsSeed,
                rbacRoleGrantsSeed,
                rbacGroupSeed
            );
        }

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

    public RbacInMemoryTestFixture(
        InMemoryDatabaseFactory inMemoryDatabaseFactory,
        SelfServiceDbContext selfServiceDbContext,
        ApiApplication apiApplication
    )
    {
        InMemoryDatabaseFactory = inMemoryDatabaseFactory;
        DbContext = selfServiceDbContext;
        ApiApplication = apiApplication;
    }
}

public class TestRbacApplicationService
{
    internal static async void PopulateRbac(
        SelfServiceDbContext dbContext,
        List<RbacPermissionGrant>? rbacPermissionGrantsSeed,
        List<RbacRoleGrant>? rbacRoleGrantsSeed,
        List<RbacGroup>? rbacGroupSeed
    )
    {
        if (rbacPermissionGrantsSeed != null)
        {
            dbContext.RbacPermissionGrants.AddRange(rbacPermissionGrantsSeed);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            dbContext.RbacPermissionGrants.Add(
                new RbacPermissionGrant(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "andfris@dfds.com",
                    @namespace: RbacNamespace.Topics,
                    permission: "create",
                    type: RbacAccessType.Capability,
                    resource: "sandbox-emcla-pmyxn"
                )
            );

            dbContext.RbacPermissionGrants.Add(
                new RbacPermissionGrant(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "test03@dfds.cloud",
                    @namespace: RbacNamespace.Topics,
                    permission: "create",
                    type: RbacAccessType.Capability,
                    resource: "sandbox-emcla-pmyxn"
                )
            );

            await dbContext.SaveChangesAsync();
        }

        if (rbacRoleGrantsSeed != null)
        {
            dbContext.RbacRoleGrants.AddRange(rbacRoleGrantsSeed);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            dbContext.RbacRoleGrants.Add(
                new RbacRoleGrant(
                    id: RbacRoleGrantId.New(),
                    roleId: RbacRoleId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "andfris@dfds.com",
                    type: RbacAccessType.Capability,
                    resource: "sandbox-emcla-pmyxn"
                )
            );

            await dbContext.SaveChangesAsync();
        }

        if (rbacGroupSeed != null)
        {
            dbContext.RbacGroups.AddRange(rbacGroupSeed);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            var ceUsersGroup = new RbacGroup(
                RbacGroupId.Parse("507531CF-6740-4728-ACDB-C3B4CEF11B27"),
                DateTime.Now,
                DateTime.Now,
                "ce - users",
                "Members of Cloud Engineering"
            );
            ceUsersGroup.Members.Add(
                new RbacGroupMember(RbacGroupMemberId.New(), DateTime.Now, ceUsersGroup.Id, "test03@dfds.cloud")
            );
            dbContext.RbacGroups.Add(ceUsersGroup);

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
            await rbacSvc.IsUserPermitted(
                "emcla@dfds.com",
                [new Permission { Namespace = RbacNamespace.Topics, Name = "create" }],
                "sandbox-emcla-pmyxn"
            ),
            await rbacSvc.IsUserPermitted(
                "emcla@dfds.com",
                [
                    new Permission { Namespace = RbacNamespace.Topics, Name = "create" },
                    new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" },
                ],
                "sandbox-emcla-pmyxn"
            ),
            await rbacSvc.IsUserPermitted(
                "emcla@dfds.com",
                [new Permission { Namespace = RbacNamespace.Topics, Name = "read-public" }],
                "sandbox-emcla-pmyxn"
            ),
            await rbacSvc.IsUserPermitted(
                "andfris@dfds.com",
                [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                "sandbox-emcla-pmyxn"
            ),
            await rbacSvc.IsUserPermitted(
                "andfris@dfds.com",
                [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                "andfris-sandbox-6-aeyex"
            ),
        };

        cases.ForEach(c => Console.WriteLine(c.Permitted()));
    }

    [Fact]
    public async void UserAllow()
    {
        // var test01GroupId = Guid.NewGuid();
        // await using var databaseFactory = new InMemoryDatabaseFactory();
        // var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        // CreateTestRbacApplicationService(dbContext, [
        //     new Group
        //     {
        //         Id = test01GroupId,
        //         Name = "test01 - users",
        //         Members = ["test01@dfds.cloud"]
        //     }
        // ], [
        //     new AccessPolicy {
        //         AccessType = AccessType.Capability,
        //         ObjectIds = ["test01"],
        //         Entities = [new Entity {EntityType = EntityType.Group, Id = test01GroupId.ToString()}, new Entity {EntityType = EntityType.User, Id = "test01@dfds.cloud"}],
        //         Accesses = [new Access {Permissions = [
        //             new Permission { Namespace = RbacNamespace.Topics, Name = "create"},
        //             new Permission { Namespace = RbacNamespace.Topics, Name = "read-private"}
        //         ]}]
        //     },
        //     new AccessPolicy {
        //         AccessType = AccessType.Capability,
        //         ObjectIds = ["test02"],
        //         Entities = [new Entity {EntityType = EntityType.Group, Id = test01GroupId.ToString()}, new Entity {EntityType = EntityType.User, Id = "test03@dfds.cloud"}, new Entity {EntityType = EntityType.User, Id = "test01@dfds.cloud"}],
        //         Accesses = [new Access {Permissions = [
        //             new Permission { Namespace = RbacNamespace.Topics, Name = "read-public"}
        //         ]}]
        //     }
        // ]);

        var fixture = await RbacTestData.NewInMemoryFixture(
            true,
            new List<RbacPermissionGrant>
            {
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "test01@dfds.cloud",
                    @namespace: RbacNamespace.Topics,
                    permission: "create",
                    type: RbacAccessType.Capability,
                    resource: "test01"
                ),
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "test01@dfds.cloud",
                    @namespace: RbacNamespace.Topics,
                    permission: "read-public",
                    type: RbacAccessType.Capability,
                    resource: "test02"
                ),
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "test01@dfds.cloud",
                    @namespace: RbacNamespace.Topics,
                    permission: "read-private",
                    type: RbacAccessType.Capability,
                    resource: "test01"
                ),
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.Group,
                    assignedEntityId: "507531CF-6740-4728-ACDB-C3B4CEF11B27",
                    @namespace: RbacNamespace.Topics,
                    permission: "read-public",
                    type: RbacAccessType.Capability,
                    resource: "test02"
                ),
            },
            new List<RbacRoleGrant>()
        );
        var rbacSvc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "test01@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "create" }],
                    "test01"
                )
            ).Permitted()
        );
        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "test01@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "test01"
                )
            ).Permitted()
        );
        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "test01@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-public" }],
                    "test02"
                )
            ).Permitted()
        );
        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "test03@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-public" }],
                    "test02"
                )
            ).Permitted()
        );
        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "test04@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-public" }],
                    "test02"
                )
            ).Permitted()
        );
    }

    [Fact]
    public async void UserDeny()
    {
        var test01GroupId = Guid.NewGuid();

        var fixture = await RbacTestData.NewInMemoryFixture(
            true,
            new List<RbacPermissionGrant>
            {
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "test01@dfds.cloud",
                    @namespace: RbacNamespace.Topics,
                    permission: "create",
                    type: RbacAccessType.Capability,
                    resource: "test01"
                ),
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "test01@dfds.cloud",
                    @namespace: RbacNamespace.Topics,
                    permission: "read-private",
                    type: RbacAccessType.Capability,
                    resource: "test01"
                ),
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "test01@dfds.cloud",
                    @namespace: RbacNamespace.Topics,
                    permission: "read-public",
                    type: RbacAccessType.Capability,
                    resource: "test02"
                ),
            },
            new List<RbacRoleGrant>()
        );
        var rbacSvc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "test02@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "create" }],
                    "test01"
                )
            ).Permitted()
        );
        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "test02@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "test01"
                )
            ).Permitted()
        );
        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "test02@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-public" }],
                    "test02"
                )
            ).Permitted()
        );
        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "test01@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-public" }],
                    "test01"
                )
            ).Permitted()
        );
        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "test01@dfds.cloud",
                    [new Permission { Namespace = RbacNamespace.CapabilityManagement, Name = "request-deletion" }],
                    "test01"
                )
            ).Permitted()
        );
    }

    [Fact]
    public async void UserWithTopicCreateCanCreate()
    {
        var fixture = await RbacTestData.NewInMemoryFixture(
            true,
            new List<RbacPermissionGrant>
            {
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "emcla@dfds.com",
                    @namespace: RbacNamespace.Topics,
                    permission: "create",
                    type: RbacAccessType.Capability,
                    resource: "sandbox-emcla-pmyxn"
                ),
            },
            new List<RbacRoleGrant>()
        );
        var rbacSvc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "emcla@dfds.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "create" }],
                    "sandbox-emcla-pmyxn"
                )
            ).Permitted()
        );
    }

    [Fact]
    public async void UserWithTopicReadCanNotCreate()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var application = await new ApiApplicationBuilder()
            .WithSelfServiceDbContext(dbContext)
            .ConfigureRbac()
            .BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "emclaa@dfds.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "create" }],
                    "sandbox-emcla-pmyxn"
                )
            ).Permitted()
        );
    }

    [Fact]
    public async void UserWithTopicReadPrivateCanReadPrivate()
    {
        var fixture = await RbacTestData.NewInMemoryFixture(
            true,
            new List<RbacPermissionGrant>
            {
                new(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: "emcla@dfds.com",
                    @namespace: RbacNamespace.Topics,
                    permission: "read-private",
                    type: RbacAccessType.Capability,
                    resource: "sandbox-emcla-pmyxn"
                ),
            },
            new List<RbacRoleGrant>()
        );
        var rbacSvc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "emcla@dfds.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "sandbox-emcla-pmyxn"
                )
            ).Permitted()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void UserAllowUsingDbStore()
    {
        var application = await new ApiApplicationBuilder().WithLocalDb().ConfigureRbac().BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        // var dbContext = application.Services.GetService<SelfServiceDbContext>()!;
        //
        // var permissions = dbContext.RbacPermissionGrants.ToList();
        // var roles = dbContext.RbacRoleGrants.ToList();
        // var rbacGroups = await dbContext.RbacGroups.ToListAsync();

        Assert.NotNull(rbacSvc);

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "emcla@dfds.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "sandbox-emcla-pmyxn"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "emcla@dfds.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "sandbox-throwaway-01"
                )
            ).Permitted()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void CapabilityOwnerAccessUsingDbStore()
    {
        var application = await new ApiApplicationBuilder().WithLocalDb().ConfigureRbac().BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        // var dbContext = application.Services.GetService<SelfServiceDbContext>()!;
        //
        // var permissions = dbContext.RbacPermissionGrants.ToList();
        // var roles = dbContext.RbacRoleGrants.ToList();
        // var rbacGroups = await dbContext.RbacGroups.ToListAsync();

        Assert.NotNull(rbacSvc);

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "owner@bar.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "sandbox-emcla-pmyxn"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "owner@bar.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "owner@bar.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "create" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "owner@bar.com",
                    [new Permission { Namespace = RbacNamespace.Rbac, Name = "create" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "owner@bar.com",
                    [new Permission { Namespace = RbacNamespace.CapabilityMembershipManagement, Name = "create" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "owner@bar.com",
                    [new Permission { Namespace = RbacNamespace.CapabilityMembershipManagement, Name = "delete" }],
                    "bar"
                )
            ).Permitted()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void CapabilityContributorAccessUsingDbStore()
    {
        var application = await new ApiApplicationBuilder().WithLocalDb().ConfigureRbac().BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        // var dbContext = application.Services.GetService<SelfServiceDbContext>()!;
        //
        // var permissions = dbContext.RbacPermissionGrants.ToList();
        // var roles = dbContext.RbacRoleGrants.ToList();
        // var rbacGroups = await dbContext.RbacGroups.ToListAsync();

        Assert.NotNull(rbacSvc);

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "contributor@bar.com",
                    [new Permission { Namespace = RbacNamespace.CapabilityManagement, Name = "delete" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "contributor@bar.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "contributor@bar.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "create" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "contributor@bar.com",
                    [new Permission { Namespace = RbacNamespace.Rbac, Name = "create" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "contributor@bar.com",
                    [new Permission { Namespace = RbacNamespace.CapabilityMembershipManagement, Name = "create" }],
                    "bar"
                )
            ).Permitted()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void CapabilityReaderAccessUsingDbStore()
    {
        var application = await new ApiApplicationBuilder().WithLocalDb().ConfigureRbac().BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        // var dbContext = application.Services.GetService<SelfServiceDbContext>()!;
        //
        // var permissions = dbContext.RbacPermissionGrants.ToList();
        // var roles = dbContext.RbacRoleGrants.ToList();
        // var rbacGroups = await dbContext.RbacGroups.ToListAsync();

        Assert.NotNull(rbacSvc);

        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "reader@bar.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "bar"
                )
            ).Permitted()
        );

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "reader@bar.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "some-other-capability"
                )
            ).Permitted()
        );

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "reader@bar.com",
                    [new Permission { Namespace = RbacNamespace.CapabilityManagement, Name = "delete" }],
                    "bar"
                )
            ).Permitted()
        );
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async void CapabilityOtherAccessUsingDbStore()
    {
        var application = await new ApiApplicationBuilder().WithLocalDb().ConfigureRbac().BuildAsync();
        var rbacSvc = application.Services.GetService<IRbacApplicationService>()!;
        // var dbContext = application.Services.GetService<SelfServiceDbContext>()!;
        //
        // var permissions = dbContext.RbacPermissionGrants.ToList();
        // var roles = dbContext.RbacRoleGrants.ToList();
        // var rbacGroups = await dbContext.RbacGroups.ToListAsync();

        Assert.NotNull(rbacSvc);

        Assert.False(
            (
                await rbacSvc.IsUserPermitted(
                    "other@foo.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-private" }],
                    "bar"
                )
            ).Permitted()
        );

        /*
            Reading public topics is allowed for everyone
            This is currently handled outside of RBAC in the application logic
            [04-11-2025, andfris] Leaving this test here as a reminder
        */
        /*
        Assert.True(
            (
                await rbacSvc.IsUserPermitted(
                    "other@foo.com",
                    [new Permission { Namespace = RbacNamespace.Topics, Name = "read-public" }],
                    "bar"
                )
            ).Permitted()
        );
        */
    }
}
