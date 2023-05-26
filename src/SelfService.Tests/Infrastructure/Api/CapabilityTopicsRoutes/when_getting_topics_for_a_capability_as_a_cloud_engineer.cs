using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.CapabilityTopicsRoutes;

public class when_getting_topics_for_a_capability_as_a_cloud_engineer : IAsyncLifetime
{
    private readonly KafkaTopic _aPrivateTopic = A.KafkaTopic
        .WithId(KafkaTopicId.New())
        .WithName("im-private")
        .Build();

    private readonly KafkaTopic _aPublicTopic = A.KafkaTopic
        .WithId(KafkaTopicId.New())
        .WithName("pub.im-public")
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability));
        application.ReplaceService<IKafkaClusterRepository>(Dummy.Of<IKafkaClusterRepository>());
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository(_aPrivateTopic, _aPublicTopic));

        application.ConfigureFakeAuthentication(options =>
        {
            options.Role = "Cloud.Engineer";
        });

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/capabilities/foo/topics");
    }

    [Fact]
    public async Task then_items_only_include_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var nameValues = document?.SelectElements("items")
            .Select(x => x.SelectElement("name"))
            .Select(x => x?.GetString() ?? "");

        Assert.Equal(new[] { _aPublicTopic.Name.ToString() }, nameValues!);
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items").Single();

        var values = topicItem?
            .SelectElements("_links/self/allow")?
            .Select(x => x.GetString())
            .ToArray();

        Assert.Equal(new[] { "GET", "DELETE" }, values);
    }

    public Task DisposeAsync()
    {
        _response?.Dispose();
        return Task.CompletedTask;
    }
}