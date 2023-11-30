using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityMembersRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_members_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery())
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/members/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/members", hrefValue);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_members_link_when_NOT_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery())
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/members/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_members_link_when_is_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true))
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/members/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_members_link_when_has_pending_membership_application()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembershipApplication: true))
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/members/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }
}
