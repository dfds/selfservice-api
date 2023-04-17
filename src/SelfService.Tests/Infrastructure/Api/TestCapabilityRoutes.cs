using System.Net;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityRoutes
{
    [Fact]
    public async Task getting_non_existing_capability_by_id_returns_not_found()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository());

        using var client = application.CreateClient();

        var response = await client.GetAsync("/capabilities/some-capability");

        Assert.Equal(
            expected: HttpStatusCode.NotFound,
            actual: response.StatusCode
        );
    }

    [Fact]
    public async Task getting_capability_by_id_returns_ok()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();

        var response = await client.GetAsync("/capabilities/some-capability");

        Assert.Equal(
            expected: HttpStatusCode.OK,
            actual: response.StatusCode
        );
    }

    [Fact]
    public async Task get_by_id_returns_expected_details()
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

        Assert.Equal(
            expected: stubCapability.Id,
            actual: document?.SelectElement("/id")?.GetString()
        );

        Assert.Equal(
            expected: stubCapability.Name,
            actual: document?.SelectElement("/name")?.GetString()
        );

        Assert.Equal(
            expected: stubCapability.Description,
            actual: document?.SelectElement("/description")?.GetString()
        );
    }

    [Fact]
    public async Task get_by_id_returns_expected_href_on_self_link()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();
        
        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();

        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/self/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}", hrefValue);
    }

    [Fact]
    public async Task get_by_id_returns_expected_allow_on_self_link()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new StubAwsAccountRepository());
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/self/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[]{"GET"}, allowValues);
    }
}