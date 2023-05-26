using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.CapabilityTopicsRoutes;

public class when_getting_topics_for_a_capability_as_ANY_user : IAsyncLifetime
{
    private readonly Capability _aCapability = A.Capability.Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(_aCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery());
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository());
        application.ReplaceService<IKafkaClusterRepository>(Dummy.Of<IKafkaClusterRepository>());

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/capabilities/{_aCapability.Id}/topics");
    }   

    [Fact]
    public async Task then_returns_expected_href_on_self_link()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElement("/_links/self/href")?.GetString();

        Assert.EndsWith($"/capabilities/{_aCapability.Id}/topics", value);
    }

    public Task DisposeAsync()
    {
        _response!.Dispose();
        return Task.CompletedTask;
    }
}