using System.Text.Json;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityMembershipsRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_member_non_owner_leave()
    {
        var stubCapability = A.Capability.Build();
        var readerRole = A.RbacRole.WithName("Reader").WithAccessType(RbacAccessType.Capability).Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true))
            .ConfigureRbac()
            .Build();

        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());
        application.ReplaceService<IRbacApplicationService>(
            new StubRbacApplicationService(
                assignableRoles: [readerRole],
                roleGrants:
                [
                    A
                        .RbacRoleGrant.WithRoleId(readerRole.Id)
                        .AssignToUser("foo@bar.com")
                        .AssignedForCapability(stubCapability.Id)
                        .Build(),
                ],
                isPermitted: false // doesn't matter in this case
            )
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/leaveCapability/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(["GET", "POST"], allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expectedn_non_member_non_leave()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true))
            .ConfigureRbac()
            .Build();

        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: false));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/leaveCapability/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(["GET"], allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_forbids_owner_leave_with_single_owner()
    {
        var stubCapability = A.Capability.Build();
        var ownerRole = A.RbacRole.WithName("Owner").WithAccessType(RbacAccessType.Capability).Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(
            new StubRbacApplicationService(
                assignableRoles: [ownerRole],
                roleGrants:
                [
                    A
                        .RbacRoleGrant.WithRoleId(ownerRole.Id)
                        .AssignToUser("foo@bar.com")
                        .AssignedForCapability(stubCapability.Id)
                        .Build(),
                ],
                isPermitted: false // doesn't matter in this case
            )
        );
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/leaveCapability/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_allows_owner_leave_with_multiple_owners()
    {
        var stubCapability = A.Capability.Build();
        var ownerRole = A.RbacRole.WithName("Owner").WithAccessType(RbacAccessType.Capability).Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(
            new StubRbacApplicationService(
                assignableRoles: [ownerRole],
                roleGrants:
                [
                    A
                        .RbacRoleGrant.WithRoleId(ownerRole.Id)
                        .AssignToUser("foo@bar.com")
                        .AssignedForCapability(stubCapability.Id)
                        .Build(),
                    A
                        .RbacRoleGrant.WithRoleId(ownerRole.Id)
                        .AssignToUser("bar@foo.com")
                        .AssignedForCapability(stubCapability.Id)
                        .Build(),
                ],
                isPermitted: false // doesn't matter in this case
            )
        );
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/leaveCapability/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET", "POST" }, allowValues);
    }

    [Fact]
    public async Task resource_links_dont_contain_post_if_capability_is_pending_deletion()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true, hasMultipleMembers: true))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: false));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/clusters/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }
}
