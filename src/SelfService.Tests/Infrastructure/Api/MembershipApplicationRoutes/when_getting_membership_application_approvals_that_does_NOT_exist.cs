using System.Net;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.MembershipApplicationRoutes;

public class when_getting_membership_application_approvals_that_does_NOT_exist : IAsyncLifetime
{
    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<IMembershipApplicationQuery>(new StubMembershipApplicationQuery());
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/membershipapplications/{MembershipApplicationId.New()}/approvals");
    }

    [Fact]
    public void then_returns_not_found()
    {
        Assert.Equal(HttpStatusCode.NotFound, _response.StatusCode);
    }

    public Task DisposeAsync()
    {
        _response!.Dispose();
        return Task.CompletedTask;
    }
}
