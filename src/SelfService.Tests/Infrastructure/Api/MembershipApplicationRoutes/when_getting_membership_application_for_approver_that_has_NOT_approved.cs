using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.MembershipApplicationRoutes;

public class when_getting_membership_application_for_approver_that_has_NOT_approved : IAsyncLifetime
{
    private const string SomeApprover = "some-approver";

    private readonly MembershipApplication _aMembershipApplication = A
        .MembershipApplication.WithApplicant("some-user")
        .WithApproval(builder => builder.WithApprovedBy(SomeApprover))
        .Build();

    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<IMembershipApplicationQuery>(
            new StubMembershipApplicationQuery(_aMembershipApplication)
        );
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.CapabilityMembershipManagement,
                        "read-requests",
                        RbacAccessType.Capability,
                        "foo"
                    ),
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.CapabilityMembershipManagement,
                        "manage-requests",
                        RbacAccessType.Capability,
                        "foo"
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/membershipapplications/{_aMembershipApplication.Id}");
    }

    [Fact]
    public async Task then_returns_expected_approvers()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document
            ?.SelectElements("/approvals/items")
            .Select(x => x.SelectElement("approvedBy"))
            .Select(x => x.ToString())
            .ToArray();

        Assert.Equal(new[] { SomeApprover }, value);
    }

    [Fact]
    public async Task then_returns_expected_allow()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var value = document?.SelectElements("/approvals/_links/self/allow").Select(x => x.ToString()).ToArray();

        Assert.Equal(new[] { "GET", "POST", "DELETE" }, value);
    }

    public Task DisposeAsync()
    {
        _response!.Dispose();
        return Task.CompletedTask;
    }
}
