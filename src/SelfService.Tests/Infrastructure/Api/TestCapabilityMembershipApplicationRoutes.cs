using SelfService.Domain.Models;
using System.Text.Json;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityMembershipApplicationRoutes
{
    [Fact]
    public async Task get_capability_by_id_returns_expected_href_on_membership_applications_link()
    {
        var stubCapability = A.Capability.WithId("foo").Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery())
            .Build();

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
        var stubCapability = A.Capability.WithId("foo").Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembership: true))
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElements("/_links/membershipApplications/allow")
            ?.Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_by_id_returns_expected_allow_on_membership_applications_link_when_is_NOT_member()
    {
        var stubCapability = A.Capability.WithId("foo").Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery())
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElements("/_links/membershipApplications/allow")
            ?.Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET", "POST" }, allowValues);
    }

    [Fact]
    public async Task get_capability_by_id_returns_expected_allow_on_membership_applications_link_when_is_NOT_member_but_has_submitted_membership_application()
    {
        var stubCapability = A.Capability.WithId("foo").Build();

        await using var application = new ApiApplicationBuilder()
            .WithAwsAccountRepository(new StubAwsAccountRepository())
            .WithCapabilityRepository(new StubCapabilityRepository(stubCapability))
            .WithMembershipQuery(new StubMembershipQuery(hasActiveMembershipApplication: true))
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElements("/_links/membershipApplications/allow")
            ?.Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_href_on_self_link()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery());
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery()
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var hrefValue = document?.SelectElement("/_links/self/href")?.GetString();

        Assert.EndsWith($"/capabilities/{stubCapability.Id}/membershipapplications", hrefValue);
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_allow_on_self_link_when_NOT_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: false));
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery()
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElements("/_links/self/allow")?.Select(x => x.GetString() ?? "").ToArray();

        Assert.Equal(new[] { "GET", "POST" }, allowValues);
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_allow_on_self_link_when_is_member()
    {
        var stubCapability = A.Capability.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery()
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElements("/_links/self/allow")?.Select(x => x.GetString() ?? "").ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_allow_on_self_link_when_has_active_membership_application()
    {
        var stubCapability = A.Capability.Build();
        var stubMembershipApplication = A.MembershipApplication.Build();

        await using var application = new ApiApplication();
        application.ConfigureFakeAuthentication(options =>
        {
            options.Name = stubMembershipApplication.Applicant;
        });

        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(stubCapability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembershipApplication: true));
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery(stubMembershipApplication)
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/{stubCapability.Id}/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document?.SelectElements("/_links/self/allow")?.Select(x => x.GetString() ?? "").ToArray();

        Assert.Equal(new[] { "GET" }, allowValues);
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_membership_application_details()
    {
        var stubMembershipApplication = A.MembershipApplication.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery(stubMembershipApplication)
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/foo/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var applicationJson = document!.SelectElements("/items").SingleOrDefault();

        Assert.Equal(expected: stubMembershipApplication.Id, actual: applicationJson.GetProperty("id").GetString());

        Assert.Equal(
            expected: stubMembershipApplication.Applicant,
            actual: applicationJson.GetProperty("applicant").GetString()
        );

        Assert.Equal(
            expected: stubMembershipApplication.ExpiresOn.ToUniversalTime().ToString("O"),
            actual: applicationJson.GetProperty("expiresOn").GetString()
        );

        Assert.Equal(
            expected: stubMembershipApplication.SubmittedAt.ToUniversalTime().ToString("O"),
            actual: applicationJson.GetProperty("submittedAt").GetString()
        );
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_membership_application_approvals_when_empty()
    {
        var stubMembershipApplication = A.MembershipApplication.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery(stubMembershipApplication)
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/foo/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var applicationJson = document!.SelectElements("/items").SingleOrDefault();

        Assert.Empty(applicationJson.SelectElements("/approvals/items"));
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_membership_application_approvals_href_on_self_link()
    {
        var stubMembershipApplication = A.MembershipApplication.Build();

        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery(stubMembershipApplication)
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/foo/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var applicationJson = document!.SelectElements("/items").SingleOrDefault();
        var hrefValue = applicationJson.SelectElement("/approvals/_links/self/href")?.GetString();

        Assert.EndsWith($"/membershipapplications/{stubMembershipApplication.Id}/approvals", hrefValue);
    }

    [Fact]
    public async Task get_membership_applications_for_a_capability_returns_expected_allow_on_membership_application_approvals_self_link_when_empty()
    {
        var stubMembershipApplication = A.MembershipApplication
            .WithApprovals(Enumerable.Empty<MembershipApproval>())
            .Build();

        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository(A.Capability));
        application.ReplaceService<IMembershipQuery>(new StubMembershipQuery(hasActiveMembership: true));
        application.ReplaceService<ICapabilityMembershipApplicationQuery>(
            new StubCapabilityMembershipApplicationQuery(stubMembershipApplication)
        );

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/capabilities/foo/membershipapplications");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var applicationJson = document!.SelectElements("/items").SingleOrDefault();
        var allowedVerbs = applicationJson.SelectElements("/approvals/_links/self/allow").Select(x => x.GetString());

        Assert.Equal(new[] { "GET", "POST", "DELETE" }, allowedVerbs);
    }

    //get membershipapplications for a capability returns correct allow (with variations on approvals)
}

public class StubCapabilityMembershipApplicationQuery : ICapabilityMembershipApplicationQuery
{
    private readonly MembershipApplication[] _result;

    public StubCapabilityMembershipApplicationQuery(params MembershipApplication[] result)
    {
        _result = result;
    }

    public Task<IEnumerable<MembershipApplication>> FindPendingBy(CapabilityId capabilityId)
    {
        return Task.FromResult(_result.AsEnumerable());
    }
}
