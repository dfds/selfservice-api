using System.Net;
using System.Text.Json;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.KafkaTopicRoutes;

public class TestTopicDeletionPermissions
{
    [Theory]
    [InlineData(true, "topics-public", "global", true)]
    [InlineData(true, "topics-public", "capability", true)]
    [InlineData(true, "topics", "global", false)]
    [InlineData(true, null, null, false)]
    [InlineData(false, "topics", "global", true)]
    [InlineData(false, "topics", "capability", true)]
    [InlineData(false, "topics-public", "global", false)]
    [InlineData(false, null, null, false)]
    public async Task permission_controls_the_delete_link_and_endpoint(
        bool isPublic,
        string? permissionNamespace,
        string? accessType,
        bool canDelete
    )
    {
        var topic = A.KafkaTopic.WithName(isPublic ? "pub.im-public" : "im-private").Build();
        var permissions = new List<RbacPermissionGrant>
        {
            RbacPermissionGrant.New(
                AssignedEntityType.User,
                FakeAuthenticationSchemeDefaults.DefaultUser,
                RbacNamespace.Topics,
                "read-private",
                RbacAccessType.Capability,
                topic.CapabilityId.ToString()
            ),
        };
        if (permissionNamespace != null)
        {
            permissions.Add(
                RbacPermissionGrant.New(
                    AssignedEntityType.User,
                    FakeAuthenticationSchemeDefaults.DefaultUser,
                    RbacNamespace.Parse(permissionNamespace),
                    "delete",
                    RbacAccessType.Parse(accessType!),
                    accessType == "global" ? "" : topic.CapabilityId.ToString()
                )
            );
        }
        var topicService = new Mock<IKafkaTopicApplicationService>();

        await using var application = new ApiApplication();
        application.ReplaceService(topicService.Object);
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<IKafkaTopicRepository>(new StubKafkaTopicRepository(topic));
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(permissions.ToArray())
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());
        application.ConfigureFakeAuthentication(options => options.Role = "Cloud.Engineer");

        using var client = application.CreateClient();
        using var getResponse = await client.GetAsync($"/kafkatopics/{topic.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var document = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        var allowedMethods = document
            .RootElement.GetProperty("_links")
            .GetProperty("self")
            .GetProperty("allow")
            .EnumerateArray()
            .Select(method => method.GetString());
        Assert.Equal(canDelete, allowedMethods.Contains("DELETE"));

        using var deleteResponse = await client.DeleteAsync($"/kafkatopics/{topic.Id}");
        Assert.Equal(canDelete ? HttpStatusCode.NoContent : HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
        topicService.Verify(
            service => service.DeleteKafkaTopic(topic.Id, FakeAuthenticationSchemeDefaults.DefaultUser),
            canDelete ? Times.Once() : Times.Never()
        );
    }
}
