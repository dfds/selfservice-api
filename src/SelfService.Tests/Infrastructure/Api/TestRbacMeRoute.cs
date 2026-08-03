using System.Net;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Tests.Application;

namespace SelfService.Tests.Infrastructure.Api;

public class TestRbacMeRoute
{
    [Fact]
    public async Task me_reports_the_guest_baseline_separately_from_the_users_own_grants()
    {
        var fixture = await RbacTestData.NewInMemoryFixture(
            true,
            new List<RbacPermissionGrant>(),
            new List<RbacRoleGrant>(),
            new List<RbacGroup>()
        );
        await RbacTestData.SeedGuestRole(
            fixture.DbContext,
            (RbacNamespace.ServiceCatalogue, "read", RbacAccessType.Global)
        );

        var application = fixture.ApiApplication;
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.GetAsync("/rbac/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        // The caller holds nothing of their own, but the baseline is still reported — /rbac/me would
        // understate their effective access without it.
        Assert.Empty(root.GetProperty("permissionGrants").EnumerateArray());

        var baseline = root.GetProperty("baselinePermissionGrants").EnumerateArray().ToList();
        var single = Assert.Single(baseline);
        Assert.Equal("service-catalogue", single.GetProperty("namespace").GetString());
        Assert.Equal("read", single.GetProperty("permission").GetString());
    }
}
