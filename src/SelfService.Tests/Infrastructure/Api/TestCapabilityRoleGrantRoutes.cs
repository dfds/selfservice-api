using System.Net;
using System.Text;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

/// <summary>
/// The RBAC engine is stubbed to permit everything, so these tests prove that the controller itself
/// binds the request body to the route capability instead of trusting whatever the caller posted.
/// </summary>
public class TestCapabilityRoleGrantRoutes
{
    private static readonly string RoleId = Guid.NewGuid().ToString();

    private static (ApiApplication Application, StubRbacApplicationService Rbac) NewApplication()
    {
        var rbac = new StubRbacApplicationService(isPermitted: true);
        var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability.WithId("foo")));
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(rbac);
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());
        return (application, rbac);
    }

    private static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task global_grant_is_rejected_before_reaching_the_service()
    {
        var (application, rbac) = NewApplication();
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/capabilities/foo/roles/grant",
            Body(
                $$"""
                {"roleId":"{{RoleId}}","assignedEntityType":"User","assignedEntityId":"attacker@dfds.com","type":"global","resource":""}
                """
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(rbac.GrantedRoleGrants);
    }

    [Fact]
    public async Task grant_naming_another_capability_is_rejected()
    {
        var (application, rbac) = NewApplication();
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/capabilities/foo/roles/grant",
            Body(
                $$"""
                {"roleId":"{{RoleId}}","assignedEntityType":"User","assignedEntityId":"someone@dfds.com","type":"capability","resource":"other-capability"}
                """
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(rbac.GrantedRoleGrants);
    }

    [Fact]
    public async Task grant_matching_the_route_capability_is_accepted()
    {
        var (application, rbac) = NewApplication();
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/capabilities/foo/roles/grant",
            Body(
                $$"""
                {"roleId":"{{RoleId}}","assignedEntityType":"User","assignedEntityId":"someone@dfds.com","type":"capability","resource":"foo"}
                """
            )
        );

        // Created() carries no body, which HttpNoContentOutputFormatter normalizes to 204.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var grant = Assert.Single(rbac.GrantedRoleGrants);
        Assert.Equal("foo", grant.Resource);
        Assert.Equal(RbacAccessType.Capability, grant.Type);
    }

    [Fact]
    public async Task omitted_resource_defaults_to_the_route_capability()
    {
        var (application, rbac) = NewApplication();
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/capabilities/foo/roles/grant",
            Body(
                $$"""
                {"roleId":"{{RoleId}}","assignedEntityType":"User","assignedEntityId":"someone@dfds.com","type":"capability"}
                """
            )
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var grant = Assert.Single(rbac.GrantedRoleGrants);
        Assert.Equal("foo", grant.Resource);
    }

    [Fact]
    public async Task unparseable_access_type_is_a_bad_request_not_a_server_error()
    {
        var (application, rbac) = NewApplication();
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/capabilities/foo/roles/grant",
            Body(
                $$"""
                {"roleId":"{{RoleId}}","assignedEntityType":"User","assignedEntityId":"someone@dfds.com","type":"nonsense","resource":"foo"}
                """
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(rbac.GrantedRoleGrants);
    }
}
