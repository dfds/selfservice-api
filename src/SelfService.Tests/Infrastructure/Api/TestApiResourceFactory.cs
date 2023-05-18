using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api;
using SelfService.Infrastructure.Api.Me;

namespace SelfService.Tests.Infrastructure.Api;

public class TestApiResourceFactory
{
    [Theory]
    [InlineData("some-topic", UserAccessLevelOptions.Read, new string[0])]
    [InlineData("pub.some-topic", UserAccessLevelOptions.Read, new[] { "GET" })]
    [InlineData("some-topic", UserAccessLevelOptions.ReadWrite, new[] { "GET", "POST" })]
    [InlineData("pub.some-topic", UserAccessLevelOptions.ReadWrite, new[] { "GET", "POST" })]
    public void convert_kafka_topic(string topicName, UserAccessLevelOptions accessLevel, string[] expectedAllowed)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(A.KafkaTopic.WithName(topicName), accessLevel);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Equal(expectedAllowed, result.Links.MessageContracts.Allow);
    }

    [Fact]
    public void convert_capability_list()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(new[] { A.Capability.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_capability_list_item()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.ConvertToListItem(A.Capability);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Theory]
    [InlineData(true, new[] { "GET", "POST" }, new[] { "GET", "POST" })]
    [InlineData(false, new[] { "GET" }, new string[0])]
    public async Task convert_capability(bool authorized, string[] expectedAllowed, string[] expectedAllowAwsAccount)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock.SetReturnsDefault(Task.FromResult(authorized ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), authorizationServiceMock.Object);

        var result = await sut.Convert(A.Capability);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Equal(new[] { "GET" }, result.Links.Members.Allow);
        Assert.Equal(expectedAllowed, result.Links.Topics.Allow);
        Assert.Equal(expectedAllowed, result.Links.MembershipApplications.Allow);
        Assert.Equal(expectedAllowed, result.Links.LeaveCapability.Allow);
        Assert.Equal(expectedAllowAwsAccount, result.Links.AwsAccount.Allow);
    }

    [Theory]
    [InlineData(UserAccessLevelOptions.Read, new[] { "GET" })]
    [InlineData(UserAccessLevelOptions.ReadWrite, new[] { "GET", "POST" })]
    public void convert_aws_account(UserAccessLevelOptions accessLevel, string[] expectedAllowed)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(A.AwsAccount, accessLevel);

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_kafka_cluster()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(A.KafkaCluster);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_kafka_cluster_list()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(new[] { A.KafkaCluster.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_message_contract()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(A.MessageContract);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Theory]
    [InlineData(UserAccessLevelOptions.Read, new[] { "GET" })]
    [InlineData(UserAccessLevelOptions.ReadWrite, new[] { "GET", "POST" })]
    public void convert_message_contract_list(UserAccessLevelOptions accessLevel, string[] expectedAllowed)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(new[] { A.MessageContract.Build() }, KafkaTopicId.New(), accessLevel);

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_membership_application_for_current_user()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var membershipApplication = A.MembershipApplication
            .WithApplicant("some-user")
            .WithApproval(builder => builder.WithApprovedBy("some-approver"))
            .Build();
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(membershipApplication, UserAccessLevelOptions.Read, "some-user");

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Empty(result.Approvals.Items);
        Assert.Empty(result.Approvals.Links.Self.Allow);
    }

    [Theory]
    [InlineData("some-approver", new[] { "GET" })]
    [InlineData("another-approver", new[] { "GET", "POST" })]
    public void convert_membership_application(string currentUser, string[] expectedAllowed)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var membershipApplication = A.MembershipApplication
            .WithApplicant("some-user")
            .WithApproval(builder => builder.WithApprovedBy("some-approver"))
            .Build();
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(membershipApplication, UserAccessLevelOptions.Read, currentUser);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Equal(expectedAllowed, result.Approvals.Links.Self.Allow);
        Assert.NotEmpty(result.Approvals.Items);
    }

    [Theory]
    [InlineData(false, new[] { "GET" })]
    [InlineData(true, new[] { "GET", "POST" })]
    public async Task convert_capability_topics(bool authorized, string[] expectedAllowed)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock.SetReturnsDefault(Task.FromResult(authorized ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), authorizationServiceMock.Object);
        var capabilityTopics = new CapabilityTopics(A.Capability, new[] { new ClusterTopics(A.KafkaCluster, new KafkaTopic[] { A.KafkaTopic }) });

        var result = await sut.Convert(capabilityTopics);

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
    }
    
    [Theory]
    [InlineData(UserAccessLevelOptions.Read, new[] { "GET", "POST" })]
    [InlineData(UserAccessLevelOptions.ReadWrite, new[] { "GET" })]
    public void convert_membership_application_list_no_applications(UserAccessLevelOptions accessLevel, string[] expectedAllowed)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert("some-capability", accessLevel, new MembershipApplication[0], "some-user");

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
    }

    [Theory]
    [InlineData(UserAccessLevelOptions.Read)]
    [InlineData(UserAccessLevelOptions.ReadWrite)]
    public void convert_membership_application_list(UserAccessLevelOptions accessLevel)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert("some-capability", accessLevel, new MembershipApplication[] { A.MembershipApplication }, "some-user");

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_capability_member_list()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert("some-capability", new[] { A.Member.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_public_topic_list()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert(new[] { A.KafkaTopic.Build() }, new[] { A.KafkaCluster.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public void convert_me()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(httpContextAccessorMock.Object, Mock.Of<LinkGenerator>(), Mock.Of<IAuthorizationService>());

        var result = sut.Convert("some-user", new Capability[0], A.Member, true, new Stat[0]);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Equal(new[] { "PUT" }, result.Links.PersonalInformation.Allow);
        Assert.Equal(new[] { "POST" }, result.Links.PortalVisits.Allow);
        Assert.Equal(new[] { "GET" }, result.Links.TopVisitors.Allow);
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