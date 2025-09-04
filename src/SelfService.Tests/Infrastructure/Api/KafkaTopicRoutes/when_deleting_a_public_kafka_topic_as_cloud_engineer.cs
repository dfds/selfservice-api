using System.Net;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.KafkaTopicRoutes;

public class when_deleting_a_public_kafka_topic_as_cloud_engineer : IAsyncLifetime
{
    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        var stubKafkaTopic = A.KafkaTopic.WithName("pub.im-public").Build();

        await using var application = new ApiApplication();
        application.ReplaceService(Dummy.Of<IKafkaTopicApplicationService>());
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository(stubKafkaTopic));
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Topics,
                        "delete",
                        RbacAccessType.Capability,
                        "foo"
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        application.ConfigureFakeAuthentication(options =>
        {
            options.Role = "Cloud.Engineer";
        });

        using var client = application.CreateClient();

        _response = await client.DeleteAsync($"/kafkatopics/{stubKafkaTopic.Id}");
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
