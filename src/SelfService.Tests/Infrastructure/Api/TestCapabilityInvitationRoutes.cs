using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityInvitationRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_invitations_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/sendInvitations/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/invitations", hrefValue);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_invitations_link_when_NOT_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/sendInvitations/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.True(allowValues != null);
        Assert.Empty(allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_invitations_link_when_is_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/sendInvitations/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "POST" }, allowValues);
    }
}
