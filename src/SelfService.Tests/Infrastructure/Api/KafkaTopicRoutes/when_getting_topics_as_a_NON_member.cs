using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.KafkaTopicRoutes;

public class when_getting_topics_as_a_NON_member : IAsyncLifetime
{
    private readonly Capability _aCapability = A.Capability.Build();

    private readonly KafkaTopic _aTopic = A.KafkaTopic
        .WithId(KafkaTopicId.New())
        .WithKafkaClusterId("foo")
        .WithName("pub.im-public")
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<IKafkaClusterRepository>(
            new StubKafkaClusterRepository(A.KafkaCluster.WithId("foo"))
        );
        application.ReplaceService<IKafkaTopicQuery>(new StubKafkaTopicQuery(_aTopic));

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/kafkatopics?capabilityId={_aCapability.Id}");
    }

    [Fact]
    public async Task then_self_link_of_root_has_expected_href()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElement("/_links/self/href")?.GetString();
        Assert.EndsWith($"/kafkatopics?CapabilityId={_aCapability.Id}", value);
    }

    [Fact]
    public async Task then_self_link_of_root_has_expected_allow()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var values = document?.SelectElements("/_links/self/allow").Select(x => x.GetString()).ToArray();

        Assert.Equal(new[] { "GET" }, values);
    }

    [Fact]
    public async Task then_items_only_include_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var nameValues = document
            ?.SelectElements("items")
            .Select(x => x.SelectElement("name"))
            .Select(x => x?.GetString() ?? "");

        Assert.Equal(new[] { _aTopic.Name.ToString() }, nameValues!);
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items").Single();
        var values = topicItem?.SelectElements("_links/self/allow")?.Select(x => x.GetString()).ToArray();

        Assert.Equal(new[] { "GET" }, values);
    }

    [Fact]
    public async Task then_items_has_expected_empty_update_description_link()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items").Single();

        var value = topicItem?.SelectElement("_links/updateDescription");

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
