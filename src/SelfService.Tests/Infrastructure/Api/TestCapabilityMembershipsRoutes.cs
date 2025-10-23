using System.Text.Json;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityMembershipsRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_leave_with_multiple_members()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true, hasMultipleMembers: true))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
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
    public async Task get_capability_by_id_returns_expected_leave_with_single_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
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
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
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
