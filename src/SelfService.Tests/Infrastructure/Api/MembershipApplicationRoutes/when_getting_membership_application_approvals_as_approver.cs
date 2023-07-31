using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.MembershipApplicationRoutes;

public class when_getting_membership_application_approvals_as_approver : IAsyncLifetime
{
    private readonly MembershipApplication _aMembershipApplication = A.MembershipApplication
        .WithApplicant("some-user")
        .WithApproval(builder => builder.WithApprovedBy("foo@bar.com"))
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<IMembershipApplicationQuery>(
            new StubMembershipApplicationQuery(_aMembershipApplication)
        );

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/membershipapplications/{_aMembershipApplication.Id}/approvals");
    }

    [Fact]
    public async Task then_returns_expected_approvers()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document
            ?.SelectElements("/items")
            .Select(x => x.SelectElement("approvedBy"))
            .Select(x => x.ToString())
            .ToArray();

        Assert.Equal(new[] { "foo@bar.com" }, value);
    }

    [Fact]
    public async Task then_returns_expected_allow()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElements("/_links/self/allow").Select(x => x.ToString()).ToArray();

        Assert.Equal(new[] { "GET" }, value);
    }

    public Task DisposeAsync()
    {
        _response!.Dispose();
        return Task.CompletedTask;
    }
}
