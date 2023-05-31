using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api;
using SelfService.Infrastructure.Api.Authorization;
using SelfService.Infrastructure.Api.Me;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class ApiResourceFactoryBuilder
{
    private IAuthorizationService _authorizationService;
    private SelfService.Domain.Services.IAuthorizationService _domainAuthorizationService;

    public ApiResourceFactoryBuilder()
    {
        _authorizationService = Mock.Of<IAuthorizationService>();
        _domainAuthorizationService = Dummy.Of<SelfService.Domain.Services.IAuthorizationService>();
    }

    public ApiResourceFactoryBuilder WithAuthorizationService(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
        return this;
    }

    public ApiResourceFactoryBuilder WithDomainAuthorizationService(SelfService.Domain.Services.IAuthorizationService domainAuthorizationService)
    {
        _domainAuthorizationService = domainAuthorizationService;
        return this;
    }

    public ApiResourceFactory Build()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock
            .SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "foo")
                }))
            });

        return new ApiResourceFactory(
            httpContextAccessor: httpContextAccessorMock.Object,
            linkGenerator: Mock.Of<LinkGenerator>(),
            authorizationService: _authorizationService,
            domainAuthorizationService: _domainAuthorizationService
        );
    }

    public static implicit operator ApiResourceFactory(ApiResourceFactoryBuilder builder)
        => builder.Build();
}


public class TestApiResourceFactory
{
    [Theory(Skip = "4later")]
    [InlineData("some-topic", new string[0])]
    [InlineData("pub.some-topic", new[] { "GET" })]
    [InlineData("some-topic", new[] { "GET", "POST" })]
    [InlineData("pub.some-topic", new[] { "GET", "POST" })]
    public async Task convert_kafka_topic(string topicName, string[] expectedAllowed)
    {
        var authorizationMock = new Mock<SelfService.Domain.Services.IAuthorizationService>();
        authorizationMock
            .Setup(x => x.CanReadMessageContracts(It.IsAny<PortalUser>(), It.IsAny<KafkaTopic>()))
            .ReturnsAsync(true);

        var sut = new ApiResourceFactoryBuilder()
            .WithDomainAuthorizationService(authorizationMock.Object)
            .Build();

        var topic = A.KafkaTopic.WithName(topicName).Build();

        var result = await sut.Convert(topic);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
        Assert.Equal(expectedAllowed, result.Links.MessageContracts.Allow);
    }

    [Fact]
    public async Task convert_capability_list()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert(new[] { A.Capability.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public async Task convert_capability_list_item()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.ConvertToListItem(A.Capability);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Theory]
    [InlineData(true, new[] { "GET", "POST" }, new[] { "GET", "POST" })]
    [InlineData(false, new[] { "GET" }, new string[0])]
    public async Task convert_capability(bool authorized, string[] expectedAllowed, string[] expectedAllowAwsAccount)
    {
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock.SetReturnsDefault(Task.FromResult(authorized ? AuthorizationResult.Success() : AuthorizationResult.Failed()));

        var sut = new ApiResourceFactoryBuilder()
            .WithAuthorizationService(authorizationServiceMock.Object)
            .Build();

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
    public async Task convert_aws_account(UserAccessLevelOptions accessLevel, string[] expectedAllowed)
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert(A.AwsAccount, accessLevel);

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
    }

    [Fact]
    public async Task convert_kafka_cluster()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert(A.KafkaCluster);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public async Task convert_kafka_cluster_list()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert(new[] { A.KafkaCluster.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public async Task convert_message_contract()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert(A.MessageContract);

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Theory(Skip = "4later")]
    [InlineData(UserAccessLevelOptions.Read, new[] { "GET" })]
    [InlineData(UserAccessLevelOptions.ReadWrite, new[] { "GET", "POST" })]
    public async Task convert_message_contract_list(UserAccessLevelOptions accessLevel, string[] expectedAllowed)
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        //var result = await sut.Convert(new[] { A.MessageContract.Build() }, KafkaTopicId.New(),  accessLevel);
        var result = await sut.Convert(new[] { A.MessageContract.Build() }, A.KafkaTopic);

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
    }

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

    [Theory]
    [InlineData(UserAccessLevelOptions.Read, new[] { "GET", "POST" })]
    [InlineData(UserAccessLevelOptions.ReadWrite, new[] { "GET" })]
    public async Task convert_membership_application_list_no_applications(UserAccessLevelOptions accessLevel, string[] expectedAllowed)
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert("some-capability", accessLevel, new MembershipApplication[0], "some-user");

        Assert.Equal(expectedAllowed, result.Links.Self.Allow);
    }

    [Theory]
    [InlineData(UserAccessLevelOptions.Read)]
    [InlineData(UserAccessLevelOptions.ReadWrite)]
    public async Task convert_membership_application_list(UserAccessLevelOptions accessLevel)
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert("some-capability", accessLevel, new MembershipApplication[] { A.MembershipApplication }, "some-user");

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public async Task convert_capability_member_list()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
        var result = sut.Convert("some-capability", new[] { A.Member.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact(Skip = "4later")]
    public async Task convert_public_topic_list()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(new DefaultHttpContext());
        var sut = new ApiResourceFactory(
            httpContextAccessorMock.Object,
            Mock.Of<LinkGenerator>(),
            Mock.Of<IAuthorizationService>(),
            Dummy.Of<SelfService.Domain.Services.IAuthorizationService>()
        );

        var result = await sut.Convert(new[] { A.KafkaTopic.Build() }, new[] { A.KafkaCluster.Build() });

        Assert.Equal(new[] { "GET" }, result.Links.Self.Allow);
    }

    [Fact]
    public async Task convert_me()
    {
        var sut = new ApiResourceFactoryBuilder().Build();
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