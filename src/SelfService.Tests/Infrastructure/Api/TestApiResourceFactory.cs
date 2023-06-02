using SelfService.Domain.Models;

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
}