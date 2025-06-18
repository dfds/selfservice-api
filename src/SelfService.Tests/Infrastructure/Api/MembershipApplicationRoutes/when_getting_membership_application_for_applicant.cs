using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.MembershipApplicationRoutes;

public class when_getting_membership_application_for_applicant : IAsyncLifetime
{
    private readonly MembershipApplication _aMembershipApplication = A
        .MembershipApplication.WithApplicant("foo@bar.com")
        .WithApproval(builder => builder.WithApprovedBy("some-approver"))
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<IMembershipApplicationQuery>(
            new StubMembershipApplicationQuery(_aMembershipApplication)
        );
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/membershipapplications/{_aMembershipApplication.Id}");
    }

    [Fact]
    public async Task then_returns_expected_href_on_self_link()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElement("/_links/self/href")?.GetString();

        Assert.EndsWith($"/membershipapplications/{_aMembershipApplication.Id}", value);
    }

    [Fact]
    public async Task then_returns_no_approvals()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElements("/approvals/items").ToArray();

        Assert.Equal(Array.Empty<JsonElement>(), value);
    }

    [Fact]
    public async Task then_returns_no_allows_for_approval_link()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElements("/approvals/_links/self/allow").Select(x => x.GetString()).ToArray();

        Assert.Equal(Array.Empty<string>(), value);
    }

    public Task DisposeAsync()
    {
        _response!.Dispose();
        return Task.CompletedTask;
    }
}
