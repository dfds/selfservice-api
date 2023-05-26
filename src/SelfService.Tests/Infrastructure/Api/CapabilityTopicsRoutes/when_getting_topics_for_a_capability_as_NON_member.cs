using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.CapabilityTopicsRoutes;

public class when_getting_topics_for_a_capability_as_NON_member : IAsyncLifetime
{
    private readonly Capability _aCapability = A.Capability.Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        var stubPublicTopic = A.KafkaTopic
            .WithId(KafkaTopicId.New())
            .WithName("pub.im-public")
            .Build();

        var stubPrivateTopic = A.KafkaTopic
            .WithId(KafkaTopicId.New())
            .WithName("im-private")
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(_aCapability));
        application.ReplaceService<IKafkaClusterRepository>(Dummy.Of<IKafkaClusterRepository>());
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository(stubPublicTopic, stubPrivateTopic));

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/capabilities/{_aCapability.Id}/topics");
    }

    [Fact]
    public async Task then_self_link_of_root_has_expected_href()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElement("/_links/self/href")?.GetString();
        Assert.EndsWith($"/capabilities/{_aCapability.Id}/topics", value);
    }

    [Fact]
    public async Task then_self_link_of_root_has_expected_allow()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var values = document?.SelectElements("/_links/self/allow")
            .Select(x => x.GetString())
            .ToArray();

        Assert.Equal(new[] { "GET" }, values);
    }

    [Fact]
    public async Task then_items_only_include_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var nameValues = document?.SelectElements("items")
            .Select(x => x.SelectElement("name"))
            .Select(x => x?.GetString() ?? "");
            
        Assert.Equal(new[] { "pub.im-public" }, nameValues!);
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items").Single();
        var values = topicItem?
            .SelectElements("_links/self/allow")?
            .Select(x => x.GetString())
            .ToArray();

        Assert.Equal(new[] { "GET" }, values);
    }

    [Fact]
    public async Task then_items_has_expected_empty_update_description_link()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items").Single();
        var value = topicItem?.SelectElement("_links/update-description");

        if (value?.ValueKind == JsonValueKind.Null)
        {
            value = null;
        }

        Assert.Null(value);
    }

    public Task DisposeAsync()
    {
        _response?.Dispose();
        return Task.CompletedTask;
    }
}