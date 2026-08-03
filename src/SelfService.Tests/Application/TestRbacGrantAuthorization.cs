using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

/// <summary>
/// Authorization tests for <see cref="IRbacApplicationService.GrantRoleGrant"/> when the grant comes
/// from a caller-supplied request body (<c>userInitiated: true</c>).
/// </summary>
public class TestRbacGrantAuthorization
{
    private const string Owner = "owner@bar.com";
    private const string CloudEngineer = "ce@dfds.com";
    private const string Nobody = "nobody@dfds.com";
    private const string Bystander = "someone@dfds.com";
    private const string Bar = "bar";
    private const string OtherCapability = "other-capability";

    private class Scenario
    {
        public required RbacInMemoryTestFixture Fixture { get; init; }
        public required IRbacApplicationService Service { get; init; }

        /// <summary>Mirrors the seeded Owner role: capability manage-permissions plus rbac/create.</summary>
        public required RbacRoleId OwnerRoleId { get; init; }
        public required RbacRoleId CloudEngineerRoleId { get; init; }
        public required RbacRoleId ReaderRoleId { get; init; }
        public required RbacRoleId GuestRoleId { get; init; }
    }

    private static async Task<Scenario> NewScenario()
    {
        // Seed explicitly rather than through PopulateRbac's arguments: that helper is `async void` and
        // is not awaited by NewInMemoryFixture, so anything assertions depend on must be added here.
        var fixture = await RbacTestData.NewInMemoryFixture(
            populateDatabase: true,
            rbacPermissionGrantsSeed: new List<RbacPermissionGrant>(),
            rbacRoleGrantsSeed: new List<RbacRoleGrant>(),
            rbacGroupSeed: new List<RbacGroup>()
        );

        var db = fixture.DbContext;

        var ownerRole = RbacRole.New("system", "Owner", "", RbacAccessType.Global);
        var cloudEngineerRole = RbacRole.New("system", "CloudEngineer", "", RbacAccessType.Global);
        var readerRole = RbacRole.New("system", "Reader", "", RbacAccessType.Global);
        var guestRole = RbacRole.New("system", "Guest", "", RbacAccessType.Global);

        db.RbacRoles.AddRange(ownerRole, cloudEngineerRole, readerRole, guestRole);

        db.RbacPermissionGrants.AddRange(
            RbacPermissionGrant.New(
                AssignedEntityType.Role,
                ownerRole.Id.ToString(),
                RbacNamespace.Capability,
                "manage-permissions",
                RbacAccessType.Capability,
                ""
            ),
            // The Owner role really does carry rbac/create — that is what made finding #1 exploitable.
            RbacPermissionGrant.New(
                AssignedEntityType.Role,
                ownerRole.Id.ToString(),
                RbacNamespace.Rbac,
                "create",
                RbacAccessType.Global,
                ""
            ),
            RbacPermissionGrant.New(
                AssignedEntityType.Role,
                cloudEngineerRole.Id.ToString(),
                RbacNamespace.Rbac,
                "create",
                RbacAccessType.Global,
                ""
            )
        );

        db.RbacRoleGrants.AddRange(
            RbacRoleGrant.New(ownerRole.Id, AssignedEntityType.User, Owner, RbacAccessType.Capability, Bar),
            RbacRoleGrant.New(cloudEngineerRole.Id, AssignedEntityType.User, CloudEngineer, RbacAccessType.Global, "")
        );

        await db.SaveChangesAsync();

        return new Scenario
        {
            Fixture = fixture,
            Service = fixture.ApiApplication.Services.GetService<IRbacApplicationService>()!,
            OwnerRoleId = ownerRole.Id,
            CloudEngineerRoleId = cloudEngineerRole.Id,
            ReaderRoleId = readerRole.Id,
            GuestRoleId = guestRole.Id,
        };
    }

    private static RbacRoleGrant Grant(RbacRoleId roleId, string assignedTo, RbacAccessType type, string? resource) =>
        new(RbacRoleGrantId.New(), roleId, DateTime.Now, AssignedEntityType.User, assignedTo, type, resource!);

    // The proxied TransactionalAspect doesn't apply when ConfigureRbac re-registers the service without
    // rewiring, so changes live in the DbContext tracker until we flush them.
    private static async Task<List<RbacRoleGrant>> PersistedGrants(Scenario scenario)
    {
        await scenario.Fixture.DbContext.SaveChangesAsync();
        return await scenario.Fixture.DbContext.RbacRoleGrants.ToListAsync();
    }

    [Fact]
    public async Task capability_owner_cannot_create_a_global_role_grant()
    {
        // Regression test for security-review-01 finding #1: a capability Owner holds rbac/create, but
        // only through a Capability-scoped role grant, so it must not authorize a global grant.
        var scenario = await NewScenario();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () =>
                await scenario.Service.GrantRoleGrant(
                    Owner,
                    Grant(scenario.CloudEngineerRoleId, Owner, RbacAccessType.Global, ""),
                    userInitiated: true
                )
        );
    }

    [Fact]
    public async Task global_admin_can_create_a_global_role_grant()
    {
        var scenario = await NewScenario();

        await scenario.Service.GrantRoleGrant(
            CloudEngineer,
            Grant(scenario.CloudEngineerRoleId, Bystander, RbacAccessType.Global, ""),
            userInitiated: true
        );

        var grants = await PersistedGrants(scenario);
        Assert.Contains(grants, g => g.AssignedEntityId == Bystander && g.Type == RbacAccessType.Global);
    }

    [Fact]
    public async Task capability_manager_cannot_grant_on_another_capability()
    {
        var scenario = await NewScenario();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () =>
                await scenario.Service.GrantRoleGrant(
                    Owner,
                    Grant(scenario.ReaderRoleId, Bystander, RbacAccessType.Capability, OtherCapability),
                    userInitiated: true
                )
        );
    }

    [Fact]
    public async Task capability_manager_can_grant_to_another_user_on_own_capability()
    {
        var scenario = await NewScenario();

        await scenario.Service.GrantRoleGrant(
            Owner,
            Grant(scenario.ReaderRoleId, Bystander, RbacAccessType.Capability, Bar),
            userInitiated: true
        );

        var grants = await PersistedGrants(scenario);
        Assert.Contains(grants, g => g.AssignedEntityId == Bystander && g.Resource == Bar);
    }

    [Fact]
    public async Task capability_manager_cannot_grant_to_themselves()
    {
        var scenario = await NewScenario();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () =>
                await scenario.Service.GrantRoleGrant(
                    Owner,
                    Grant(scenario.OwnerRoleId, Owner, RbacAccessType.Capability, Bar),
                    userInitiated: true
                )
        );
    }

    [Fact]
    public async Task global_admin_can_grant_to_themselves()
    {
        var scenario = await NewScenario();

        await scenario.Service.GrantRoleGrant(
            CloudEngineer,
            Grant(scenario.OwnerRoleId, CloudEngineer, RbacAccessType.Capability, Bar),
            userInitiated: true
        );

        var grants = await PersistedGrants(scenario);
        Assert.Contains(grants, g => g.AssignedEntityId == CloudEngineer && g.Resource == Bar);
    }

    [Fact]
    public async Task user_without_permissions_cannot_grant_anything()
    {
        var scenario = await NewScenario();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () =>
                await scenario.Service.GrantRoleGrant(
                    Nobody,
                    Grant(scenario.ReaderRoleId, Bystander, RbacAccessType.Capability, Bar),
                    userInitiated: true
                )
        );
    }

    [Fact]
    public async Task system_initiated_grants_are_not_authorized()
    {
        // Mirror of the finding #1 test with the flag defaulted. This pins the contract the five
        // bootstrap call sites rely on — flipping the default must fail loudly here.
        var scenario = await NewScenario();

        await scenario.Service.GrantRoleGrant(
            Owner,
            Grant(scenario.CloudEngineerRoleId, Owner, RbacAccessType.Global, "")
        );

        var grants = await PersistedGrants(scenario);
        Assert.Contains(grants, g => g.AssignedEntityId == Owner && g.Type == RbacAccessType.Global);
    }

    [Fact]
    public async Task bulk_grants_stay_on_the_trusted_path()
    {
        var scenario = await NewScenario();

        await scenario.Service.GrantRoleGrants(
            Nobody,
            new List<RbacRoleGrant>
            {
                Grant(scenario.ReaderRoleId, Bystander, RbacAccessType.Capability, Bar),
                Grant(scenario.ReaderRoleId, Bystander, RbacAccessType.Capability, OtherCapability),
            }
        );

        var grants = await PersistedGrants(scenario);
        Assert.Contains(grants, g => g.AssignedEntityId == Bystander && g.Resource == Bar);
        Assert.Contains(grants, g => g.AssignedEntityId == Bystander && g.Resource == OtherCapability);
    }

    [Fact]
    public async Task guest_role_cannot_be_granted_at_capability_scope()
    {
        var scenario = await NewScenario();

        await Assert.ThrowsAsync<BadHttpRequestException>(
            async () =>
                await scenario.Service.GrantRoleGrant(
                    Owner,
                    Grant(scenario.GuestRoleId, Bystander, RbacAccessType.Capability, Bar),
                    userInitiated: true
                )
        );
    }

    [Fact]
    public async Task capability_grant_without_resource_is_rejected()
    {
        var scenario = await NewScenario();

        // Granted by a global admin so the request reaches the switch's own validation.
        await Assert.ThrowsAsync<BadHttpRequestException>(
            async () =>
                await scenario.Service.GrantRoleGrant(
                    CloudEngineer,
                    Grant(scenario.ReaderRoleId, Bystander, RbacAccessType.Capability, null),
                    userInitiated: true
                )
        );
    }
}
