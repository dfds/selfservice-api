using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.CapabilityTopicsRoutes;

public class when_getting_topics_for_a_capability_as_member : IAsyncLifetime
{
    private readonly Capability _aCapability = A.Capability.Build();

    private readonly KafkaTopic _aPrivateTopic = A.KafkaTopic
        .WithId(KafkaTopicId.New())
        .WithKafkaClusterId("foo")
        .WithName("im-private")
        .Build();

    private readonly KafkaTopic _aPublicTopic = A.KafkaTopic
        .WithId(KafkaTopicId.New())
        .WithKafkaClusterId("foo")
        .WithName("pub.im-public")
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(_aCapability));
        application.ReplaceService<IKafkaClusterRepository>(new StubKafkaClusterRepository(A.KafkaCluster.WithId("foo")));
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository(_aPrivateTopic, _aPublicTopic));
        application.ReplaceService<IKafkaClusterAccessRepository>(Dummy.Of<IKafkaClusterAccessRepository>());


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

        Assert.Equal(new[] { "GET", "POST" }, values);
    }

    [Fact]
    public async Task then_items_contains_expected_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var values = document?.SelectElements("items")?
            .SelectMany(x => x.SelectElements("topics"))
            .Select(x => x.GetProperty("id").GetString())
            .ToArray();

        Assert.Equal(
            expected: new[]
            {
                _aPrivateTopic.Id.ToString(),
                _aPublicTopic.Id.ToString(),
            },
            actual: values
        );
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Where(x => x.SelectElement("id")?.GetString() == _aPublicTopic.Id)
            .Single();

        var values = topicItem?
            .SelectElements("_links/self/allow")?
            .Select(x => x.GetString())
            .ToArray();

        Assert.Equal(new[] { "GET" }, values);
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link_for_private_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Where(x => x.SelectElement("id")?.GetString() == _aPrivateTopic.Id)
            .Single();

        var values = topicItem?
            .SelectElements("_links/self/allow")?
            .Select(x => x.GetString())
            .ToArray();

        Assert.Equal(new[] { "GET", "DELETE" }, values);
    }

    [Fact]
    public async Task then_items_has_expected_expected_href_on_update_description_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Where(x => x.SelectElement("id")?.GetString() == _aPublicTopic.Id)
            .Single();

        var value = topicItem?.SelectElement("_links/updateDescription/href")?.GetString();

        Assert.EndsWith($"/kafkatopics/{_aPublicTopic.Id}/description", value);
    }

    [Fact]
    public async Task then_items_has_expected_expected_method_on_update_description_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Where(x => x.SelectElement("id")?.GetString() == _aPublicTopic.Id)
            .Single();

        var value = topicItem?.SelectElement("_links/updateDescription/method")?.GetString();

        Assert.Equal("PUT", value);
    }

    [Fact]
    public async Task then_items_has_expected_expected_href_on_update_description_link_for_private_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Where(x => x.SelectElement("id")?.GetString() == _aPrivateTopic.Id)
            .Single();

        var value = topicItem?.SelectElement("_links/updateDescription/href")?.GetString();

        Assert.EndsWith($"/kafkatopics/{_aPrivateTopic.Id}/description", value);
    }

    [Fact]
    public async Task then_items_has_expected_expected_method_on_update_description_link_for_private_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Where(x => x.SelectElement("id")?.GetString() == _aPrivateTopic.Id)
            .Single();

        var value = topicItem?.SelectElement("_links/updateDescription/method")?.GetString();

        Assert.Equal("PUT", value);
    }

    public Task DisposeAsync()
    {
        _response?.Dispose();
        return Task.CompletedTask;
    }
}