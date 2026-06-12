using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestRbacBulkAndMembers
{
    [Fact]
    public async Task GrantPermissions_bulk_creates_all_on_success()
    {
        var fixture = await RbacTestData.NewInMemoryFixture(
            populateDatabase: true,
            rbacRoleGrantsSeed: new List<RbacRoleGrant>(),
            rbacGroupSeed: new List<RbacGroup>(),
            rbacPermissionGrantsSeed: new List<RbacPermissionGrant>()
        );
        var svc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;

        var grants = new List<RbacPermissionGrant>
        {
            RbacPermissionGrant.New(
                AssignedEntityType.User,
                "alice@dfds.com",
                RbacNamespace.SystemLegacy,
                "read",
                RbacAccessType.Global,
                ""
            ),
            RbacPermissionGrant.New(
                AssignedEntityType.User,
                "bob@dfds.com",
                RbacNamespace.SystemLegacy,
                "read",
                RbacAccessType.Global,
                ""
            ),
        };

        await svc.GrantPermissions("ce@dfds.com", grants);

        // Test scope: the proxied TransactionalAspect doesn't apply when ConfigureRbac re-registers the
        // service without rewiring, so changes live in the DbContext tracker until we flush them.
        await fixture.DbContext.SaveChangesAsync();

        var allGrants = await fixture.DbContext.RbacPermissionGrants.ToListAsync();
        Assert.Equal(2, allGrants.Count);
        Assert.Contains(allGrants, g => g.AssignedEntityId == "alice@dfds.com");
        Assert.Contains(allGrants, g => g.AssignedEntityId == "bob@dfds.com");
    }

    [Fact]
    public async Task GrantPermissions_bulk_rolls_back_atomically_on_failure()
    {
        var fixture = await RbacTestData.NewInMemoryFixture(
            populateDatabase: true,
            rbacRoleGrantsSeed: new List<RbacRoleGrant>(),
            rbacGroupSeed: new List<RbacGroup>(),
            rbacPermissionGrantsSeed: new List<RbacPermissionGrant>()
        );
        var svc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;

        // A grant with an undefined access type triggers an exception inside GrantPermission's switch.
        var validGrant = RbacPermissionGrant.New(
            AssignedEntityType.User,
            "alice@dfds.com",
            RbacNamespace.SystemLegacy,
            "read",
            RbacAccessType.Global,
            ""
        );
        // Aws/Azure access types are not handled by GrantPermission's switch and trigger the default throw.
        var invalidGrant = RbacPermissionGrant.New(
            AssignedEntityType.User,
            "bob@dfds.com",
            RbacNamespace.SystemLegacy,
            "read",
            RbacAccessType.Aws,
            ""
        );

        await Assert.ThrowsAnyAsync<Exception>(
            async () =>
                await svc.GrantPermissions("ce@dfds.com", new List<RbacPermissionGrant> { validGrant, invalidGrant })
        );
    }

    [Fact]
    public async Task GrantGroupGrant_accepts_service_principal_member()
    {
        var groupId = RbacGroupId.New();
        var seedGroup = new RbacGroup(groupId, DateTime.Now, DateTime.Now, "test-group", "");

        var fixture = await RbacTestData.NewInMemoryFixture(
            populateDatabase: true,
            rbacPermissionGrantsSeed: new List<RbacPermissionGrant>(),
            rbacRoleGrantsSeed: new List<RbacRoleGrant>(),
            rbacGroupSeed: new List<RbacGroup> { seedGroup }
        );
        var svc = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!;

        var spId = Guid.NewGuid().ToString();
        var sp = Member.RegisterServicePrincipal(UserId.Parse(spId), $"{spId}@service.local", "Worker SP");
        await fixture.DbContext.Members.AddAsync(sp);
        await fixture.DbContext.SaveChangesAsync();

        var membership = RbacGroupMember.New(groupId, spId);
        await svc.GrantGroupGrant("ce@dfds.com", membership);
        await fixture.DbContext.SaveChangesAsync();

        var persisted = await fixture.DbContext.RbacGroupMembers.ToListAsync();
        Assert.Single(persisted);
        Assert.Equal(spId, persisted[0].UserId);
    }
}
