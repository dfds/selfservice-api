using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestTopicDeletionPermissionConfiguration
{
    [Fact]
    public void public_topic_deletion_is_in_the_permission_catalogue()
    {
        var permission = Assert.Single(
            Permission.BootstrapPermissions(),
            p => p.Namespace == RbacNamespace.TopicsPublic && p.Name == "delete"
        );

        Assert.Equal(RbacAccessType.Capability, permission.AccessType);
    }
}
