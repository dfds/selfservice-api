using System.Net;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.MembershipApplicationRoutes;

public class when_getting_membership_application_for_another_applicant_as_NON_member : IAsyncLifetime
{
    private readonly MembershipApplication _aMembershipApplication = A.MembershipApplication
        .WithApplicant("some-user")
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<IMembershipApplicationQuery>(new StubMembershipApplicationQuery(_aMembershipApplication));

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/membershipapplications/{_aMembershipApplication.Id}");
    }

    [Fact]
    public Task then_returns_unauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, _response.StatusCode);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _response!.Dispose();
        return Task.CompletedTask;
    }
}
