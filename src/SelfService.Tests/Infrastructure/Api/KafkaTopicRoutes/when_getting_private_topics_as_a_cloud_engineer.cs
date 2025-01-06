using System.Net;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.KafkaTopicRoutes;

public class when_getting_private_topics_as_a_cloud_engineer : IAsyncLifetime
{
    private readonly KafkaTopic _aPrivateTopic = A
        .KafkaTopic.WithId(KafkaTopicId.New())
        .WithKafkaClusterId("foo")
        .WithName("im-private")
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<IKafkaClusterRepository>(
            new StubKafkaClusterRepository(A.KafkaCluster.WithId("foo"))
        );
        application.ReplaceService<IKafkaTopicQuery>(new StubKafkaTopicQuery(_aPrivateTopic));

        application.ConfigureFakeAuthentication(options =>
        {
            options.Role = "Cloud.Engineer";
        });

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/kafkatopics");
    }

    [Fact]
    public void then_response_status_code_is_expected()
    {
        Assert.Equal((HttpStatusCode)200, _response.StatusCode);
    }

    [Fact]
    public async Task then_items_contains_expected_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var nameValues = document
            ?.SelectElements("items")
            .Select(x => x.SelectElement("name"))
            .Select(x => x?.GetString() ?? "");

        Assert.Equal(new[] { _aPrivateTopic.Name.ToString() }, nameValues!);
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document?.SelectElements("items").Single();

        var values = topicItem?.SelectElements("_links/self/allow")?.Select(x => x.GetString()).ToArray();

        Assert.Equal(new[] { "GET" }, values);
    }

    public Task DisposeAsync()
    {
        _response?.Dispose();
        return Task.CompletedTask;
    }
}
