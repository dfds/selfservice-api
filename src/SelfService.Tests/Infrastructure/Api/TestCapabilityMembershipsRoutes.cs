using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityMembershipsRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_leave_with_multiple_members()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(
            new StubMembershipQuery(hasActiveMembership: true, hasMultipleMembers: true)
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/leaveCapability/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET", "POST" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_leave_with_single_member()
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
            ?.SelectElement("/_links/leaveCapability/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }
}
