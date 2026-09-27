using System.Net;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Infrastructure.CopilotInsights;
using SelfService.Tests.Application;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCopilotRoutes
{
    private const string Caller = "jane.doe@dfds.com";

    private sealed record Setup(ApiApplication Application, FakeCopilotInsightsClient Client) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Application.DisposeAsync();
    }

    private static async Task<Setup> NewApplication(bool readAll = false, bool guestReadOwn = true)
    {
        var grants = new List<RbacPermissionGrant>();
        if (readAll)
        {
            grants.Add(
                new RbacPermissionGrant(
                    id: RbacPermissionGrantId.New(),
                    createdAt: DateTime.Now,
                    assignedEntityType: AssignedEntityType.User,
                    assignedEntityId: Caller,
                    @namespace: RbacNamespace.Copilot,
                    permission: "read-all",
                    type: RbacAccessType.Global,
                    resource: ""
                )
            );
        }

        var fixture = await RbacTestData.NewInMemoryFixture(
            true,
            grants,
            new List<RbacRoleGrant>(),
            new List<RbacGroup>()
        );
        if (guestReadOwn)
        {
            await RbacTestData.SeedGuestRole(
                fixture.DbContext,
                (RbacNamespace.Copilot, "read-own", RbacAccessType.Global)
            );
        }

        var client = new FakeCopilotInsightsClient();
        var application = fixture.ApiApplication;
        application.ReplaceConfiguration("SS_MIDDLEWARE_AUTHCHECKER_DISABLE", "false");
        application.ReplaceService<ICopilotInsightsClient>(client);
        application.ConfigureFakeAuthentication(o =>
        {
            o.Name = Caller;
            o.Upn = Caller;
        });
        return new Setup(application, client);
    }

    private static CopilotResult<CopilotUserBundleDto> Bundle() =>
        new(
            new CopilotUserBundleDto
            {
                Identity = new CopilotIdentityDto
                {
                    UserId = 42,
                    Login = "user-a",
                    Seated = true,
                },
                Seats =
                {
                    new CopilotSeatDto { Scope = "enterprise:dfds", OrgName = "dfds-internal" },
                },
                Daily =
                {
                    new CopilotUserDayDto { Day = new DateOnly(2026, 9, 21), UserInitiatedInteractionCount = 3 },
                },
                Credits = new CopilotUserCreditsDto
                {
                    Totals = new CopilotCreditTotalsDto { Quantity = 10.5m, GrossAmount = 0.12m },
                    ByModel =
                    {
                        new CopilotCreditModelDto
                        {
                            Model = "gpt-5",
                            Totals = new() { Quantity = 10.5m },
                        },
                    },
                },
                Budget = new CopilotBudgetDto
                {
                    Amount = 500m,
                    Consumed = 42.5m,
                    PreventFurtherUsage = true,
                },
            },
            true,
            200,
            new CopilotMetaDto { MatchedEmail = Caller },
            null
        );

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task me_returns_the_callers_usage_for_a_guest_baseline_member()
    {
        await using var setup = await NewApplication();
        setup.Client.ByEmail.Enqueue(Bundle());
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/me?from=2026-09-01&to=2026-09-23");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.True(body.GetProperty("mapped").GetBoolean());
        Assert.Equal("user-a", body.GetProperty("login").GetString());
        Assert.Equal(3, body.GetProperty("daily")[0].GetProperty("interactions").GetInt32());
        Assert.Equal(0.12m, body.GetProperty("credits").GetProperty("totals").GetProperty("grossAmount").GetDecimal());        Assert.True(body.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
        Assert.Equal("2026-09-01", body.GetProperty("meta").GetProperty("from").GetString());
        Assert.EndsWith("/copilot/me", body.GetProperty("_links").GetProperty("self").GetProperty("href").GetString());
        var budget = body.GetProperty("budget");
        Assert.Equal(500m, budget.GetProperty("amount").GetDecimal());
        Assert.Equal(42.5m, budget.GetProperty("consumed").GetDecimal());
        Assert.True(budget.GetProperty("preventFurtherUsage").GetBoolean());

        var raw = body.GetRawText();
        Assert.DoesNotContain("netAmount", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("discount", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adoptionPhase", raw, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(new[] { Caller }, Assert.Single(setup.Client.ByEmailCalls));
        Assert.Equal((new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 23)), Assert.Single(setup.Client.ByEmailRanges));
    }

    [Fact]
    public async Task me_defaults_to_the_28_days_ending_today()
    {
        await using var setup = await NewApplication();
        setup.Client.ByEmail.Enqueue(Bundle());
        using var http = setup.Application.CreateClient();

        await http.GetAsync("/copilot/me");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Equal((today.AddDays(-27), today), Assert.Single(setup.Client.ByEmailRanges));
    }

    [Fact]
    public async Task me_unmapped_is_200_with_the_reason()
    {
        await using var setup = await NewApplication();
        setup.Client.ByEmail.Enqueue(new CopilotResult<CopilotUserBundleDto>(null, true, 404, null, "not_mapped"));
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.False(body.GetProperty("mapped").GetBoolean());
        Assert.Equal("not_mapped", body.GetProperty("unmappedReason").GetString());
        Assert.True(body.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
    }

    [Fact]
    public async Task me_upstream_unavailable_is_200_with_copilot_available_false()
    {
        await using var setup = await NewApplication();
        setup.Client.ByEmail.Enqueue(CopilotResult<CopilotUserBundleDto>.Unavailable(503));
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await Json(response)).GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
    }

    [Theory]
    [InlineData("?from=2026-13-01")]
    [InlineData("?to=yesterday")]
    [InlineData("?from=2026-09-10&to=2026-09-01")]
    [InlineData("?from=2025-01-01&to=2026-09-01")]
    public async Task me_rejects_a_bad_range(string query)
    {
        await using var setup = await NewApplication();
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/me" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(setup.Client.ByEmailCalls);
    }

    [Fact]
    public async Task me_is_forbidden_without_read_own()
    {
        await using var setup = await NewApplication(guestReadOwn: false);
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(setup.Client.ByEmailCalls);
    }

    [Fact]
    public async Task me_credits_covers_the_requested_calendar_month()
    {
        await using var setup = await NewApplication();
        setup.Client.ByEmail.Enqueue(Bundle());
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/me/credits?year=2026&month=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(2026, body.GetProperty("year").GetInt32());
        Assert.Equal(2, body.GetProperty("month").GetInt32());
        Assert.Equal("gpt-5", body.GetProperty("credits").GetProperty("byModel")[0].GetProperty("model").GetString());
        Assert.False(body.TryGetProperty("daily", out _));
        Assert.Equal((new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)), Assert.Single(setup.Client.ByEmailRanges));
    }

    [Theory]
    [InlineData("?month=13")]
    [InlineData("?month=0")]
    [InlineData("?year=1999&month=1")]
    public async Task me_credits_rejects_a_bad_month(string query)
    {
        await using var setup = await NewApplication();
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/me/credits" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task admin_is_forbidden_for_a_plain_user()
    {
        await using var setup = await NewApplication();
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/admin/summary?scope=enterprise:dfds");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(setup.Client.ProxyCalls);
    }

    [Fact]
    public async Task admin_passes_through_for_a_read_all_holder()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.Proxy.Enqueue(
            new CopilotProxyResponse(true, 200, """{"data":{"seats":{"total":179}},"meta":{}}""")
        );
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/admin/summary?scope=enterprise%3Adfds&from=2026-09-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await Json(response);
        Assert.Equal(179, body.GetProperty("data").GetProperty("seats").GetProperty("total").GetInt32());
        Assert.True(body.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
        Assert.Equal("summary?scope=enterprise%3Adfds&from=2026-09-01", Assert.Single(setup.Client.ProxyCalls));
    }

    [Fact]
    public async Task admin_preserves_the_upstream_status()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.Proxy.Enqueue(new CopilotProxyResponse(true, 404, """{"error":"not_mapped","detail":"x"}"""));
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/admin/users/by-email/nobody@dfds.com");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_mapped", (await Json(response)).GetProperty("error").GetString());
        Assert.Equal("users/by-email/nobody%40dfds.com", Assert.Single(setup.Client.ProxyCalls));
    }

    [Fact]
    public async Task admin_upstream_unavailable_is_200_with_copilot_available_false()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.Proxy.Enqueue(CopilotProxyResponse.Unavailable(502));
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/admin/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
        Assert.False(body.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
    }

    [Theory]
    [InlineData("/copilot/admin/admin/collect/members")]
    [InlineData("/copilot/admin/healthz")]
    [InlineData("/copilot/admin/users%2F..%2Fadmin")]
    [InlineData("/copilot/admin/users/%2E%2E/admin/collect/members")]
    [InlineData("/copilot/admin/")]
    public async Task admin_rejects_paths_outside_the_allowlist(string path)
    {
        await using var setup = await NewApplication(readAll: true);
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(setup.Client.ProxyCalls);
    }

    [Fact]
    public async Task me_links_user_search_only_for_a_read_all_holder()
    {
        await using var plain = await NewApplication();
        plain.Client.ByEmail.Enqueue(Bundle());
        using var plainHttp = plain.Application.CreateClient();
        await using var admin = await NewApplication(readAll: true);
        admin.Client.ByEmail.Enqueue(Bundle());
        using var adminHttp = admin.Application.CreateClient();

        var plainLinks = (await Json(await plainHttp.GetAsync("/copilot/me"))).GetProperty("_links");
        var adminLinks = (await Json(await adminHttp.GetAsync("/copilot/me"))).GetProperty("_links");

        Assert.False(plainLinks.TryGetProperty("users", out _));
        Assert.EndsWith("/copilot/users", adminLinks.GetProperty("users").GetProperty("href").GetString());
    }

    [Theory]
    [InlineData("/copilot/users?q=user")]
    [InlineData("/copilot/users/user-a")]
    [InlineData("/copilot/users/user-a/credits")]
    public async Task users_routes_are_forbidden_for_a_plain_user(string path)
    {
        await using var setup = await NewApplication();
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(setup.Client.ByLoginCalls);
        Assert.Empty(setup.Client.SearchCalls);
    }

    [Fact]
    public async Task users_search_returns_summaries()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.Search.Enqueue(
            new CopilotResult<List<CopilotIdentitySummaryDto>>(
                new List<CopilotIdentitySummaryDto>
                {
                    new()
                    {
                        Login = "user-a",
                        Name = "User A",
                        PrimaryEmail = "a@dfds.com",
                        Seated = true,
                    },
                },
                true,
                200,
                null,
                null
            )
        );
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/users?q=%20user%20&limit=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        var item = body.GetProperty("items")[0];
        Assert.Equal("user-a", item.GetProperty("login").GetString());
        Assert.Equal("a@dfds.com", item.GetProperty("primaryEmail").GetString());
        Assert.True(item.GetProperty("seated").GetBoolean());
        Assert.True(body.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
        Assert.Equal(("user", 25), Assert.Single(setup.Client.SearchCalls));
    }

    [Fact]
    public async Task users_search_upstream_unavailable_is_200_with_copilot_available_false()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.Search.Enqueue(CopilotResult<List<CopilotIdentitySummaryDto>>.Unavailable(503));
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/users?q=user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        Assert.False(body.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
    }

    [Theory]
    [InlineData("")]
    [InlineData("?q=a")]
    [InlineData("?q=%20a%20")]
    public async Task users_search_rejects_a_short_query(string query)
    {
        await using var setup = await NewApplication(readAll: true);
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/users" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(setup.Client.SearchCalls);
    }

    [Fact]
    public async Task user_returns_the_me_shape_for_a_login()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.ByLogin.Enqueue(Bundle());
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/users/user-a?from=2026-09-01&to=2026-09-23");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.True(body.GetProperty("mapped").GetBoolean());
        Assert.Equal("user-a", body.GetProperty("login").GetString());
        var links = body.GetProperty("_links");
        Assert.EndsWith("/copilot/users/user-a", links.GetProperty("self").GetProperty("href").GetString());
        Assert.EndsWith("/copilot/users/user-a/credits", links.GetProperty("credits").GetProperty("href").GetString());
        Assert.False(links.TryGetProperty("users", out _));
        Assert.Equal("user-a", Assert.Single(setup.Client.ByLoginCalls));
        Assert.Empty(setup.Client.ByEmailCalls);
    }

    [Fact]
    public async Task user_unknown_login_is_200_with_user_not_found()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.ByLogin.Enqueue(new CopilotResult<CopilotUserBundleDto>(null, true, 404, null, "user_not_found"));
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/users/ghost");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.False(body.GetProperty("mapped").GetBoolean());
        Assert.Equal("user_not_found", body.GetProperty("unmappedReason").GetString());
    }

    [Theory]
    [InlineData("/copilot/users/a.b")]
    [InlineData("/copilot/users/a%40dfds.com")]
    [InlineData("/copilot/users/a_b/credits")]
    [InlineData("/copilot/users/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task user_rejects_a_login_github_would_not_issue(string path)
    {
        await using var setup = await NewApplication(readAll: true);
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(setup.Client.ByLoginCalls);
    }

    [Fact]
    public async Task user_credits_covers_the_requested_calendar_month()
    {
        await using var setup = await NewApplication(readAll: true);
        setup.Client.ByLogin.Enqueue(Bundle());
        using var http = setup.Application.CreateClient();

        var response = await http.GetAsync("/copilot/users/user-a/credits?year=2026&month=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(2, body.GetProperty("month").GetInt32());
        Assert.Equal("gpt-5", body.GetProperty("credits").GetProperty("byModel")[0].GetProperty("model").GetString());
    }

    [Fact]
    public async Task admin_is_get_only()
    {
        await using var setup = await NewApplication(readAll: true);
        using var http = setup.Application.CreateClient();

        var response = await http.PostAsync("/copilot/admin/admin/collect/members", null);

        Assert.True(
            response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"got {response.StatusCode}"
        );
        Assert.Empty(setup.Client.ProxyCalls);
    }
}
