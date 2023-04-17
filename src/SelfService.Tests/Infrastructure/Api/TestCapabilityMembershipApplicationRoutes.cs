using SelfService.Domain.Models;
using System.Text.Json;
using static SelfService.Tests.Infrastructure.Api.TestCapabilityController;
using SelfService.Domain.Queries;

namespace SelfService.Tests.Infrastructure.Api;

public class MembershipQueryStub : IMembershipQuery
{
    private readonly bool _hasActiveMembership;
    private readonly bool _hasActiveMembershipApplication;

    public MembershipQueryStub(bool hasActiveMembership = false, bool hasActiveMembershipApplication = false)
    {
        _hasActiveMembership = hasActiveMembership;
        _hasActiveMembershipApplication = hasActiveMembershipApplication;
    }

    public Task<bool> HasActiveMembership(UserId userId, CapabilityId capabilityId) 
        => Task.FromResult(_hasActiveMembership);

    public Task<bool> HasActiveMembershipApplication(UserId userId, CapabilityId capabilityId) 
        => Task.FromResult(_hasActiveMembershipApplication);
}

public class TestCapabilityRoutes
{
    [Fact]
    public async Task get_by_id_returns_expected_details()
    {
        var stubCapability = A.Capability.Build();
        
        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        Assert.Equal(
            expected: stubCapability.Id,
            actual: document?.SelectElement("/id")?.GetString()
        );

        Assert.Equal(
            expected: stubCapability.Name,
            actual: document?.SelectElement("/name")?.GetString()
        );

        Assert.Equal(
            expected: stubCapability.Description,
            actual: document?.SelectElement("/description")?.GetString()
        );
    }

    [Fact]
    public async Task get_by_id_returns_expected_href_on_self_link()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();
        
        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();

        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/self/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}", hrefValue);
    }

    [Fact]
    public async Task get_by_id_returns_expected_allow_on_self_link()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/self/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[]{"GET"}, allowValues);
    }
}

public class TestCapabilityMembershipApplicationRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_membership_applications_link()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/membershipApplications/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/membershipapplications", hrefValue);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_membership_applications_link_when_is_member()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/membershipApplications/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[]{"GET"}, allowValues);
    }

    [Fact]
    public async Task get_by_id_returns_expected_allow_on_membership_applications_link_when_is_NOT_member()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/membershipApplications/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[]{"GET", "POST"}, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_membership_applications_link_when_is_NOT_member_but_has_submitted_membership_application()
    {
        var stubCapability = A.Capability
            .WithId("foo")
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembershipApplication: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/membershipApplications/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[]{"GET"}, allowValues);
    }
}

public class TestCapabilityMembersRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_members_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/members/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/members", hrefValue);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_members_link_when_NOT_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/members/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_members_link_when_is_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/members/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_members_link_when_has_pending_membership_application()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembershipApplication: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/members/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

}

public class TestCapabilityTopicsRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_topics_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/topics/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/topics", hrefValue);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_topics_link_when_NOT_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: false));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/topics/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_topics_link_when_is_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/topics/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET", "POST" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_topics_link_when_has_pending_membership_application()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembershipApplication: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/topics/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

}

public class TestCapabilityAwsAccountRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_aws_account_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub());

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

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: false));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/awsAccount/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Empty(allowValues!);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_is_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/awsAccount/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET", "POST" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_has_pending_membership_application()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembershipApplication: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/awsAccount/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Empty(allowValues!);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_aws_account_link_when_is_member_and_already_has_an_aws_account()
    {
        var stubCapability = A.Capability.Build();
        var awsAccountStub = A.AwsAccount.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub(awsAccountStub));
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(stubCapability));
        application.ReplaceService<IMembershipQuery>(new MembershipQueryStub(hasActiveMembership: true));

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElement("/_links/awsAccount/allow")?
            .EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }
}
