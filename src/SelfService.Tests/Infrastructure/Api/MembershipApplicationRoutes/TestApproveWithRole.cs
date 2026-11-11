using System.Net;
using System.Text;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.MembershipApplicationRoutes;

public class TestApproveWithRole
{
    private static readonly string RoleId = Guid.NewGuid().ToString();

    private readonly MembershipApplication _application = A
        .MembershipApplication.WithApplicant("applicant@example.com")
        .Build();

    private (ApiApplication, StubRbacApplicationService, Mock<IMembershipApplicationService>) NewApplication(
        bool canGrant
    )
    {
        var rbac = new StubRbacApplicationService(isPermitted: canGrant);
        var membershipApplicationService = new Mock<IMembershipApplicationService>();
        var authorization = new Mock<IAuthorizationService>();
        authorization
            .Setup(x => x.CanApproveMembershipApplications(It.IsAny<UserId>(), It.IsAny<MembershipApplication>()))
            .ReturnsAsync(true);

        var application = new ApiApplication();
        application.ReplaceService<IMembershipApplicationQuery>(new StubMembershipApplicationQuery(_application));
        application.ReplaceService<IMembershipApplicationService>(membershipApplicationService.Object);
        application.ReplaceService<IAuthorizationService>(authorization.Object);
        application.ReplaceService<IRbacApplicationService>(rbac);
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());
        return (application, rbac, membershipApplicationService);
    }

    private static StringContent Body(string assignedEntityId) =>
        new(
            $$"""
            {"roleId":"{{RoleId}}","assignedEntityType":"User","assignedEntityId":"{{assignedEntityId}}","type":"capability"}
            """,
            Encoding.UTF8,
            "application/json"
        );

    [Fact]
    public async Task refused_grant_leaves_the_application_unapproved()
    {
        var (application, rbac, membershipApplicationService) = NewApplication(canGrant: false);
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            $"/membershipapplications/{_application.Id}/approvals",
            Body("applicant@example.com")
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        membershipApplicationService.Verify(
            x => x.ApproveMembershipApplication(It.IsAny<MembershipApplicationId>(), It.IsAny<UserId>()),
            Times.Never
        );
        Assert.Empty(rbac.GrantedRoleGrants);
    }

    [Fact]
    public async Task grant_goes_to_the_applicant_whatever_the_body_names()
    {
        var (application, rbac, membershipApplicationService) = NewApplication(canGrant: true);
        await using var _ = application;
        using var client = application.CreateClient();

        var response = await client.PostAsync(
            $"/membershipapplications/{_application.Id}/approvals",
            Body("someone-else@example.com")
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        membershipApplicationService.Verify(
            x => x.ApproveMembershipApplication(_application.Id, It.IsAny<UserId>()),
            Times.Once
        );
        var grant = Assert.Single(rbac.GrantedRoleGrants);
        Assert.Equal("applicant@example.com", grant.AssignedEntityId);
        Assert.Equal(_application.CapabilityId.ToString(), grant.Resource);
    }
}
