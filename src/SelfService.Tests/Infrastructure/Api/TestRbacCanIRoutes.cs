using System.Net;
using System.Text;
using SelfService.Tests.Application;

namespace SelfService.Tests.Infrastructure.Api;

public class TestRbacCanIRoutes
{
    private static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task duplicate_permissions_do_not_crash_the_endpoint()
    {
        var fixture = await RbacTestData.NewInMemoryFixture();
        var application = fixture.ApiApplication;
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/rbac/can-i",
            Body(
                """
                {"permissions":[{"namespace":"topics","name":"create"},{"namespace":"topics","name":"create"}],"objectid":"sandbox-emcla-pmyxn"}
                """
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task distinct_permissions_still_work()
    {
        var fixture = await RbacTestData.NewInMemoryFixture();
        var application = fixture.ApiApplication;
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            "/rbac/can-i",
            Body(
                """
                {"permissions":[{"namespace":"topics","name":"create"},{"namespace":"topics","name":"read-private"}],"objectid":"sandbox-emcla-pmyxn"}
                """
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
