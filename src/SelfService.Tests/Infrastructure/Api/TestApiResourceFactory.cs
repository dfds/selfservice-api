using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Authorization;

namespace SelfService.Tests.Infrastructure.Api;

public class TestApiResourceFactory
{
    [Fact]
    public async Task convert_membership_application_for_current_user()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var membershipApplication = A.MembershipApplication
            .WithApplicant("some-user")
            .WithApproval(builder => builder.WithApprovedBy("some-approver"))
            .Build();

        var result = sut.Convert(membershipApplication, UserAccessLevelOptions.Read, "some-user");

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Empty(result.Approvals.Items);
        Assert.Empty(result.Approvals.Links.Self.Allow);
    }

    [Theory]
    [InlineData("some-approver", new[] { "GET" })]
    [InlineData("another-approver", new[] { "GET", "POST" })]
    public async Task convert_membership_application(string currentUser, string[] expectedAllowed)
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var membershipApplication = A.MembershipApplication
            .WithApplicant("some-user")
            .WithApproval(builder => builder.WithApprovedBy("some-approver"))
            .Build();

        var result = sut.Convert(membershipApplication, UserAccessLevelOptions.Read, currentUser);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Equal(expectedAllowed, result.Approvals.Links.Self.Allow);
        Assert.NotEmpty(result.Approvals.Items);
    }

    [Theory]
    [InlineData(false, false, new[] { "GET" }, new string[0])]
    [InlineData(false, true, new[] { "GET" }, new string[0])]
    [InlineData(true, true, new[] { "GET", "POST" }, new[] { "GET" })]
    [InlineData(true, false, new[] { "GET", "POST" }, new[] { "GET", "POST" })]
    public async Task convert_capability_topics(bool isMember, bool hasAccess, string[] expectedAllowed, string[] expectedAccessAllowed)
    {
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Capability>(), It.IsAny<IAuthorizationRequirement[]>()))
            .ReturnsAsync(isMember ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CapabilityKafkaCluster>(), It.IsAny<IAuthorizationRequirement[]>()))
            .ReturnsAsync(hasAccess ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        var sut = new ApiResourceFactoryBuilder()
            .WithAuthorizationService(authorizationServiceMock.Object)
            .Build();

        var capabilityTopics = new CapabilityTopics(A.Capability, new[] { new ClusterTopics(A.KafkaCluster, new KafkaTopic[] { A.KafkaTopic }) });
        var result = await sut.Convert(capabilityTopics);

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
        Assert.Equal(new[] { "GET" }, result.Items.First().Links.Self.Allow);
        Assert.Equal(expectedAccessAllowed, result.Items.First().Links.Access.Allow);
    }

    private static class A
    {
        public static KafkaTopicBuilder KafkaTopic => new();
        public static CapabilityBuilder Capability => new();
        public static AwsAccountBuilder AwsAccount => new();
        public static KafkaClusterBuilder KafkaCluster => new();
        public static MessageContractBuilder MessageContract => new();
        public static MembershipApplicationBuilder MembershipApplication => new();
        public static MemberBuilder Member => new();
    }
}