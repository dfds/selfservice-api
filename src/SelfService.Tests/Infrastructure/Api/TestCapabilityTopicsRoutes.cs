using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityTopicsRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_topics_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/topics/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/topics", hrefValue);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_topics_link_when_NOT_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: false));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/topics/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_topics_link_when_is_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/topics/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET", "POST" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_topics_link_when_has_pending_membership_application()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembershipApplication: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/topics/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

}