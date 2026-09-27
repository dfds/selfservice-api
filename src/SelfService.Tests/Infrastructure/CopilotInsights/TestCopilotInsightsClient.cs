using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Infrastructure.CopilotInsights;

namespace SelfService.Tests.Infrastructure.CopilotInsights;

public class TestCopilotInsightsClient
{
    private const string BundleJson = """
        {
          "data": {
            "identity": { "userId": 42, "login": "user-a", "name": "User A", "primaryEmail": "a@dfds.com",
                          "orgs": ["dfds-internal"], "emails": [{ "email": "a@dfds.com", "source": "verified", "org": "dfds-internal" }],
                          "seated": true },
            "scopes": { "seats": "enterprise:dfds", "reports": "enterprise:dfds", "credits": "enterprise:dfds" },
            "seats": [
              { "scope": "enterprise:dfds", "userId": 42, "login": "user-a", "orgName": "dfds-internal", "planType": "business",
                "createdAt": "2026-01-05T10:00:00Z", "lastActivityAt": "2026-09-22T08:00:00Z", "lastActivityEditor": "vscode/1.104",
                "lastAuthenticatedAt": "2026-09-22T07:59:00Z", "pendingCancellationDate": null }
            ],
            "daily": [
              { "day": "2026-09-21", "user_id": 42, "login": "user-a", "user_initiated_interaction_count": 12,
                "code_generation_activity_count": 30, "code_acceptance_activity_count": 9, "loc_added_sum": 120,
                "ai_credits_used": 34.5, "used_agent": true, "used_chat": true, "ides": ["vscode"],
                "languages": ["go"], "models": ["claude-sonnet-5"], "ai_adoption_phase": "exploring" }
            ],
            "latest28d": {
              "from": "2026-08-26", "to": "2026-09-22",
              "totals": { "userId": 42, "login": "user-a", "daysActive": 19, "firstDay": "2026-08-27", "lastDay": "2026-09-22",
                          "adoptionPhase": "exploring", "totals": { "loc_added_sum": 900, "used_agent": true } },
              "breakdowns": {
                "ide": [{ "breakdown": "ide", "key1": "vscode", "metrics": { "loc_added_sum": 900 } }],
                "language_model": [{ "breakdown": "language_model", "key1": "go", "key2": "claude-sonnet-5", "metrics": { "code_generation_activity_count": 30 } }]
              }
            },
            "credits": {
              "totals": { "quantity": 1234.5, "net_amount": 12.34, "gross_amount": 20.00, "discount_amount": 7.66, "input_tokens": 100 },
              "byDay": [{ "day": "2026-09-21", "totals": { "quantity": 34.5, "net_amount": 0.35, "gross_amount": 0.50 } }],
              "byModel": [{ "model": "claude-sonnet-5", "totals": { "quantity": 1234.5, "net_amount": 12.34, "gross_amount": 20.00 } }]
            },
            "premiumRequests": [{ "month": "2026-03", "model": "gpt-5", "gross_amount": 1.0 }],
            "budget": { "amount": 500, "consumed": 123.456789, "scope": "multi_user_customer", "preventFurtherUsage": true }
          },
          "meta": {
            "generatedAt": "2026-09-23T10:00:00Z",
            "freshness": { "members": "2026-09-23T09:00:00Z", "aicredit": null },
            "matchedEmail": "a@dfds.com", "scope": "enterprise:dfds", "from": "2026-08-27", "to": "2026-09-23"
          }
        }
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<HttpRequestMessage> Requests { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public StubHandler(HttpStatusCode status, string body)
            : this(_ => new HttpResponseMessage(status) { Content = new StringContent(body) }) { }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            return Task.FromResult(_respond(request));
        }
    }

    private sealed class FixedToken : ICopilotInsightsTokenProvider
    {
        private readonly string? _token;

        public FixedToken(string? token) => _token = token;

        public Task<string?> GetAccessToken(CancellationToken cancellationToken = default) => Task.FromResult(_token);
    }

    private static CopilotInsightsClient Client(
        StubHandler handler,
        string? endpoint = "http://copilot-insights:8080",
        string? token = null
    ) =>
        new(
            new HttpClient(handler),
            new CopilotInsightsConfig(endpoint, ""),
            new FixedToken(token),
            NullLogger<CopilotInsightsClient>.Instance
        );

    private static readonly DateOnly From = new(2026, 8, 27);
    private static readonly DateOnly To = new(2026, 9, 23);

    [Fact]
    public async Task by_email_sends_every_address_and_the_range()
    {
        var handler = new StubHandler(HttpStatusCode.OK, BundleJson);

        await Client(handler, token: "t0ken").GetUserByEmail(new[] { "a@dfds.com", "a.b+x@dfds.com" }, From, To);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            "http://copilot-insights:8080/api/v1/users/by-email?email=a%40dfds.com&email=a.b%2Bx%40dfds.com&from=2026-08-27&to=2026-09-23",
            request.RequestUri!.AbsoluteUri
        );
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("t0ken", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task by_email_keeps_the_endpoint_path_prefix()
    {
        var handler = new StubHandler(HttpStatusCode.OK, BundleJson);

        await Client(handler, endpoint: "https://example.test/copilot")
            .GetUserByEmail(new[] { "a@dfds.com" }, From, To);

        Assert.StartsWith(
            "https://example.test/copilot/api/v1/users/by-email?",
            Assert.Single(handler.Requests).RequestUri!.AbsoluteUri
        );
    }

    [Fact]
    public async Task by_email_sends_no_authorization_without_a_token()
    {
        var handler = new StubHandler(HttpStatusCode.OK, BundleJson);

        await Client(handler).GetUserByEmail(new[] { "a@dfds.com" }, From, To);

        Assert.Null(Assert.Single(handler.Requests).Headers.Authorization);
    }

    [Fact]
    public async Task by_email_maps_the_bundle()
    {
        var result = await Client(new StubHandler(HttpStatusCode.OK, BundleJson))
            .GetUserByEmail(new[] { "a@dfds.com" }, From, To);

        Assert.True(result.Available);
        Assert.Equal(200, result.StatusCode);
        var data = result.Data!;
        Assert.Equal(42, data.Identity.UserId);
        Assert.Equal("user-a", data.Identity.Login);
        Assert.True(data.Identity.Seated);
        Assert.Equal("enterprise:dfds", data.Scopes.Credits);

        var seat = Assert.Single(data.Seats);
        Assert.Equal("dfds-internal", seat.OrgName);
        Assert.Equal(new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc), seat.LastActivityAt?.ToUniversalTime());
        Assert.Null(seat.PendingCancellationDate);

        var day = Assert.Single(data.Daily);
        Assert.Equal(new DateOnly(2026, 9, 21), day.Day);
        Assert.Equal(12, day.UserInitiatedInteractionCount);
        Assert.Equal(120, day.LocAddedSum);
        Assert.Null(day.LocDeletedSum);
        Assert.Equal(34.5, day.AiCreditsUsed);
        Assert.True(day.UsedAgent);
        Assert.Null(day.UsedCli);
        Assert.Equal(new[] { "claude-sonnet-5" }, day.Models);

        var period = data.Latest28d!;
        Assert.Equal(new DateOnly(2026, 8, 26), period.From);
        Assert.Equal(19, period.Totals!.DaysActive);
        Assert.Equal(900, period.Totals.Totals["loc_added_sum"].GetInt32());
        Assert.True(period.Totals.Totals["used_agent"].GetBoolean());
        var languageModel = Assert.Single(period.Breakdowns["language_model"]);
        Assert.Equal("go", languageModel.Key1);
        Assert.Equal("claude-sonnet-5", languageModel.Key2);
        Assert.Equal(30, languageModel.Metrics["code_generation_activity_count"]);

        var credits = data.Credits!;
        Assert.Equal(1234.5m, credits.Totals!.Quantity);
        Assert.Equal(20.00m, credits.Totals.GrossAmount);
        Assert.Equal(0.50m, Assert.Single(credits.ByDay).Totals.GrossAmount);
        Assert.Equal("claude-sonnet-5", Assert.Single(credits.ByModel).Model);

        var budget = data.Budget!;
        Assert.Equal(500m, budget.Amount);
        Assert.Equal(123.456789m, budget.Consumed);
        Assert.True(budget.PreventFurtherUsage);

        var meta = result.Meta!;
        Assert.Equal("a@dfds.com", meta.MatchedEmail);
        Assert.Equal(new DateOnly(2026, 8, 27), meta.From);
        Assert.Null(meta.Freshness["aicredit"]);
        Assert.NotNull(meta.Freshness["members"]);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "not_mapped")]
    [InlineData(HttpStatusCode.BadRequest, "no_dfds_email")]
    public async Task by_email_unmapped_is_available_without_data(HttpStatusCode status, string code)
    {
        var body = $$"""{ "error": "{{code}}", "detail": "no GitHub login" }""";

        var result = await Client(new StubHandler(status, body)).GetUserByEmail(new[] { "a@dfds.com" }, From, To);

        Assert.True(result.Available);
        Assert.Null(result.Data);
        Assert.Equal(code, result.ErrorCode);
        Assert.Equal((int)status, result.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, """{ "error": "internal" }""")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "")]
    [InlineData(HttpStatusCode.Unauthorized, "")]
    [InlineData(HttpStatusCode.BadRequest, """{ "error": "invalid_range" }""")]
    [InlineData(HttpStatusCode.NotFound, "404 page not found")]
    [InlineData(HttpStatusCode.OK, "not json")]
    [InlineData(HttpStatusCode.OK, """{ "data": null }""")]
    public async Task by_email_failures_are_unavailable(HttpStatusCode status, string body)
    {
        var result = await Client(new StubHandler(status, body)).GetUserByEmail(new[] { "a@dfds.com" }, From, To);

        Assert.False(result.Available);
        Assert.Null(result.Data);
        Assert.Equal((int)status, result.StatusCode);
    }

    [Fact]
    public async Task by_login_escapes_the_login_and_sends_the_range()
    {
        var handler = new StubHandler(HttpStatusCode.OK, BundleJson);

        var result = await Client(handler).GetUserByLogin("user-a", From, To);

        Assert.Equal("user-a", result.Data!.Identity.Login);
        Assert.Equal(
            "http://copilot-insights:8080/api/v1/users/user-a?from=2026-08-27&to=2026-09-23",
            Assert.Single(handler.Requests).RequestUri!.AbsoluteUri
        );
    }

    [Fact]
    public async Task by_login_unknown_is_available_without_data()
    {
        var body = """{ "error": "user_not_found", "detail": "no such login" }""";

        var result = await Client(new StubHandler(HttpStatusCode.NotFound, body)).GetUserByLogin("ghost", From, To);

        Assert.True(result.Available);
        Assert.Null(result.Data);
        Assert.Equal("user_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task search_sends_q_and_limit_and_keeps_only_summary_fields()
    {
        const string body = """
            { "data": [
                { "userId": 42, "login": "user-a", "name": "User A", "primaryEmail": "a@dfds.com",
                  "orgs": ["dfds-internal"], "emails": [{ "email": "a@dfds.com", "source": "verified", "org": "dfds-internal" }],
                  "seated": true }
              ],
              "meta": { "page": { "limit": 10, "offset": 0, "total": 1 } } }
            """;
        var handler = new StubHandler(HttpStatusCode.OK, body);

        var result = await Client(handler).SearchUsers("user a+b", 10);

        Assert.True(result.Available);
        var user = Assert.Single(result.Data!);
        Assert.Equal("user-a", user.Login);
        Assert.Equal("User A", user.Name);
        Assert.Equal("a@dfds.com", user.PrimaryEmail);
        Assert.True(user.Seated);
        Assert.Equal(
            new[] { "Login", "Name", "PrimaryEmail", "Seated" },
            typeof(CopilotIdentitySummaryDto).GetProperties().Select(p => p.Name).OrderBy(n => n)
        );
        Assert.Equal(
            "http://copilot-insights:8080/api/v1/identity/users?q=user%20a%2Bb&limit=10",
            Assert.Single(handler.Requests).RequestUri!.AbsoluteUri
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "")]
    [InlineData(HttpStatusCode.Unauthorized, "")]
    [InlineData(HttpStatusCode.OK, "not json")]
    public async Task search_failures_are_unavailable(HttpStatusCode status, string body)
    {
        var result = await Client(new StubHandler(status, body)).SearchUsers("user", 10);

        Assert.False(result.Available);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task transport_failure_is_unavailable_not_thrown()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        var typed = await Client(handler).GetUserByEmail(new[] { "a@dfds.com" }, From, To);
        var proxied = await Client(handler).ProxyGet("summary", "?scope=enterprise:dfds");

        Assert.False(typed.Available);
        Assert.Equal(0, typed.StatusCode);
        Assert.False(proxied.Available);
        Assert.Equal(0, proxied.StatusCode);
    }

    [Fact]
    public async Task unconfigured_endpoint_makes_no_request()
    {
        var handler = new StubHandler(HttpStatusCode.OK, BundleJson);

        var result = await Client(handler, endpoint: "").GetUserByEmail(new[] { "a@dfds.com" }, From, To);

        Assert.False(result.Available);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task proxy_preserves_status_and_body(HttpStatusCode status)
    {
        const string body = """{"data":{"x":1},"meta":{"generatedAt":"2026-09-23T10:00:00Z"}}""";
        var handler = new StubHandler(status, body);

        var result = await Client(handler).ProxyGet("reports/users/totals", "?scope=enterprise%3Adfds&limit=5");

        Assert.True(result.Available);
        Assert.Equal((int)status, result.StatusCode);
        Assert.Equal(body, result.Body);
        Assert.Equal(
            "http://copilot-insights:8080/api/v1/reports/users/totals?scope=enterprise%3Adfds&limit=5",
            Assert.Single(handler.Requests).RequestUri!.AbsoluteUri
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task proxy_server_and_credential_errors_are_unavailable(HttpStatusCode status)
    {
        var result = await Client(new StubHandler(status, """{"error":"x"}""")).ProxyGet("summary", "");

        Assert.False(result.Available);
        Assert.Equal((int)status, result.StatusCode);
        Assert.Null(result.Body);
    }
}
