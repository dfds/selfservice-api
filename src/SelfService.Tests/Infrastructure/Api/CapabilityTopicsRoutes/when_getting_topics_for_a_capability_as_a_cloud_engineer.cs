using System.Net;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.CapabilityTopicsRoutes;

public class StubKafkaClusterRepository : IKafkaClusterRepository
{
    private readonly KafkaCluster[] _clusters;

    public StubKafkaClusterRepository(params KafkaCluster[] clusters)
    {
        _clusters = clusters;
    }
    public Task<bool> Exists(KafkaClusterId id)
    {
        throw new NotImplementedException();
    }

    public Task<KafkaCluster?> FindBy(KafkaClusterId id)
    {
        return Task.FromResult(_clusters.SingleOrDefault());
    }

    public Task<IEnumerable<KafkaCluster>> GetAll()
    {
        return Task.FromResult(_clusters.AsEnumerable());
    }
}


public class when_getting_topics_for_a_capability_as_a_cloud_engineer : IAsyncLifetime
{
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
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability));
        application.ReplaceService<IKafkaClusterRepository>(new StubKafkaClusterRepository(A.KafkaCluster.WithId("foo")));
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository(_aPrivateTopic, _aPublicTopic));
        application.ReplaceService<IKafkaClusterAccessRepository>(Dummy.Of<IKafkaClusterAccessRepository>());

        application.ConfigureFakeAuthentication(options =>
        {
            options.Role = "Cloud.Engineer";
        });

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/capabilities/foo/topics");
    }

    [Fact]
    public async Task then_response_status_code_is_expected()
    {
        Assert.Equal((HttpStatusCode) 200, _response.StatusCode);
    }

    [Fact]
    public async Task then_items_only_include_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var nameValues = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Select(x => x.SelectElement("name"))
            .Select(x => x?.GetString() ?? "");

        Assert.Equal(new[] { _aPublicTopic.Name.ToString() }, nameValues!);
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items")
            .SelectMany(x => x.SelectElements("topics"))
            .Single();

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