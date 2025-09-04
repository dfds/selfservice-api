using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.KafkaTopicRoutes;

public class when_getting_topics_as_a_member : IAsyncLifetime
{
    private readonly Capability _aCapability = A.Capability.Build();

    private readonly KafkaTopic _aPrivateTopic = A
        .KafkaTopic.WithId(KafkaTopicId.New())
        .WithKafkaClusterId("foo")
        .WithName("im-private")
        .Build();

    private readonly KafkaTopic _aPublicTopic = A
        .KafkaTopic.WithId(KafkaTopicId.New())
        .WithKafkaClusterId("foo")
        .WithName("pub.im-public")
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<IKafkaClusterRepository>(
            new StubKafkaClusterRepository(A.KafkaCluster.WithId("foo"))
        );
        application.ReplaceService<IKafkaTopicQuery>(new StubKafkaTopicQuery(_aPrivateTopic, _aPublicTopic));
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Topics,
                        "read",
                        RbacAccessType.Capability,
                        _aPrivateTopic.CapabilityId.ToString()
                    ),
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Topics,
                        "delete",
                        RbacAccessType.Capability,
                        _aPrivateTopic.CapabilityId.ToString()
                    ),
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Topics,
                        "update",
                        RbacAccessType.Capability,
                        _aPrivateTopic.CapabilityId.ToString()
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

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
    public async Task then_items_contains_expected_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var values = document?.SelectElements("items")?.Select(x => x.GetProperty("id").GetString()).ToArray();

        Assert.Equal(expected: new[] { _aPrivateTopic.Id.ToString(), _aPublicTopic.Id.ToString() }, actual: values);
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document
            ?.SelectElements("items")
            .Single(x => x.SelectElement("id")?.GetString() == _aPublicTopic.Id);

        var values = topicItem?.SelectElements("_links/self/allow")?.Select(x => x.GetString()).ToArray();

        Assert.Equal(new[] { "GET", "DELETE" }, values); // [02/09-25] WARNING: THIS IS A CHANGE
    }

    [Fact]
    public async Task then_items_has_expected_allow_on_self_link_for_private_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document
            ?.SelectElements("items")
            .Single(x => x.SelectElement("id")?.GetString() == _aPrivateTopic.Id);

        var values = topicItem?.SelectElements("_links/self/allow")?.Select(x => x.GetString()).ToArray();

        Assert.Equal(new[] { "GET", "DELETE" }, values);
    }

    [Fact]
    public async Task then_items_has_expected_expected_href_on_update_description_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document
            ?.SelectElements("items")
            .Single(x => x.SelectElement("id")?.GetString() == _aPublicTopic.Id);

        var value = topicItem?.SelectElement("_links/updateDescription/href")?.GetString();

        Assert.EndsWith($"/kafkatopics/{_aPublicTopic.Id}/description", value);
    }

    [Fact]
    public async Task then_items_has_expected_expected_method_on_update_description_link_for_public_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document
            ?.SelectElements("items")
            .Single(x => x.SelectElement("id")?.GetString() == _aPublicTopic.Id);

        var value = topicItem?.SelectElement("_links/updateDescription/method")?.GetString();

        Assert.Equal("PUT", value);
    }

    [Fact]
    public async Task then_items_has_expected_expected_href_on_update_description_link_for_private_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document
            ?.SelectElements("items")
            .Single(x => x.SelectElement("id")?.GetString() == _aPrivateTopic.Id);

        var value = topicItem?.SelectElement("_links/updateDescription/href")?.GetString();

        Assert.EndsWith($"/kafkatopics/{_aPrivateTopic.Id}/description", value);
    }

    [Fact]
    public async Task then_items_has_expected_expected_method_on_update_description_link_for_private_topics()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var topicItem = document
            ?.SelectElements("items")
            .Single(x => x.SelectElement("id")?.GetString() == _aPrivateTopic.Id);

        var value = topicItem?.SelectElement("_links/updateDescription/method")?.GetString();

        Assert.Equal("PUT", value);
    }

    public Task DisposeAsync()
    {
        _response?.Dispose();
        return Task.CompletedTask;
    }
}
