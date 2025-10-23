using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityAwsAccountRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_aws_account_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery())
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/awsAccount/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/awsaccount", hrefValue);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_NOT_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: false));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/awsAccount/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Empty(allowValues!);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_is_member_and_capability_exists()
    {
        var stubCapability = A.Capability.Build();
        var stubAwsAccount = A.AwsAccount.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository(stubAwsAccount))
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Aws,
                        "read",
                        RbacAccessType.Capability,
                        stubCapability.Id.ToString()
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/awsAccount/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_is_member_and_capability_not_exists()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.CapabilityManagement,
                        "read",
                        RbacAccessType.Capability,
                        stubCapability.Id.ToString()
                    ),
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Aws,
                        "create",
                        RbacAccessType.Capability,
                        stubCapability.Id.ToString()
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();

        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/awsAccount/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "POST" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_has_pending_membership_application()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .Build();
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: false));
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Topics,
                        "read-requests",
                        RbacAccessType.Capability,
                        stubCapability.Id.ToString()
                    ),
                ]
            )
        );
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/awsAccount/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Empty(allowValues!);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_is_member_and_already_has_an_aws_account()
    {
        var stubCapability = A.Capability.Build();
        var stubAwsAccount = A.AwsAccount.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository(stubAwsAccount))
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Aws,
                        "read",
                        RbacAccessType.Capability,
                        stubCapability.Id.ToString()
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/awsAccount/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task pending_deletion_capability_doesnt_have_POST_endpoint_on_aws_account()
    {
        var stubCapability = A.Capability.WithStatus(CapabilityStatusOptions.PendingDeletion).Build();
        var stubAwsAccount = A.AwsAccount.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository(stubAwsAccount))
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(
            new StubRbacPermissionGrantRepository(
                permissions:
                [
                    RbacPermissionGrant.New(
                        AssignedEntityType.User,
                        "foo@bar.com",
                        RbacNamespace.Aws,
                        "read",
                        RbacAccessType.Capability,
                        stubCapability.Id.ToString()
                    ),
                ]
            )
        );
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: true));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/awsAccount/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task pending_deletion_capability_doesnt_have_any_info_on_aws_account_when_not_member()
    {
        var stubCapability = A.Capability.WithStatus(CapabilityStatusOptions.PendingDeletion).Build();
        var stubAwsAccount = A.AwsAccount.Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository(stubAwsAccount))
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .Build();
        application.ReplaceService<IRbacPermissionGrantRepository>(new StubRbacPermissionGrantRepository());
        application.ReplaceService<IRbacRoleGrantRepository>(new StubRbacRoleGrantRepository());
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: false));
        application.ReplaceService<IPermissionQuery>(new StubPermissionQuery());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/awsAccount/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Debug.Assert(allowValues != null, nameof(allowValues) + " != null");
        Assert.Empty(allowValues);
    }
}
