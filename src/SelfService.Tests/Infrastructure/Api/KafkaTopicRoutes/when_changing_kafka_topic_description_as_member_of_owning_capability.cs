using System.Net;
using System.Text;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.KafkaTopicRoutes;

public class when_changing_kafka_topic_description_as_member_of_owning_capability : IAsyncLifetime
{
    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        var stubKafkaTopic = A.KafkaTopic.Build();

        await using var application = new ApiApplication();
        application.ReplaceService(Dummy.Of<IKafkaTopicApplicationService>());
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository(stubKafkaTopic));
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Topics,
                        "update",
                        RbacAccessType.Capability,
                        "foo"
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();

        _response = await client.PutAsync(
            requestUri: $"/kafkatopics/{stubKafkaTopic.Id}/description",
            content: new StringContent(
                content: @"{
                    ""description"": ""dummy-value""
                }",
                encoding: Encoding.UTF8,
                mediaType: "application/json"
            )
        );
    }

    [Fact]
    public void then_response_has_expected_status_code()
    {
        Assert.Equal((HttpStatusCode)204, _response.StatusCode);
    }

    public Task DisposeAsync()
    {
        _response?.Dispose();
        return Task.CompletedTask;
    }
}
