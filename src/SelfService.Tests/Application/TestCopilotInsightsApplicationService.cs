using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Application;
using SelfService.Infrastructure.Api.Copilot;
using SelfService.Infrastructure.CopilotInsights;

namespace SelfService.Tests.Application;

public class FakeCopilotInsightsClient : ICopilotInsightsClient
{
    public Queue<CopilotResult<CopilotUserBundleDto>> ByEmail { get; } = new();
    public Queue<CopilotProxyResponse> Proxy { get; } = new();
    public List<IReadOnlyList<string>> ByEmailCalls { get; } = new();
    public List<(DateOnly From, DateOnly To)> ByEmailRanges { get; } = new();
    public List<string> ProxyCalls { get; } = new();
    public Queue<CopilotResult<CopilotUserBundleDto>> ByLogin { get; } = new();
    public List<string> ByLoginCalls { get; } = new();
    public Queue<CopilotResult<List<CopilotIdentitySummaryDto>>> Search { get; } = new();
    public List<(string Q, int Limit)> SearchCalls { get; } = new();

    public Task<CopilotResult<CopilotUserBundleDto>> GetUserByLogin(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        ByLoginCalls.Add(login);
        return Task.FromResult(ByLogin.Dequeue());
    }

    public Task<CopilotResult<List<CopilotIdentitySummaryDto>>> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        SearchCalls.Add((q, limit));
        return Task.FromResult(Search.Dequeue());
    }

    public Task<CopilotResult<CopilotUserBundleDto>> GetUserByEmail(
        IReadOnlyList<string> emails,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        ByEmailCalls.Add(emails);
        ByEmailRanges.Add((from, to));
        return Task.FromResult(ByEmail.Dequeue());
    }

    public Task<CopilotProxyResponse> ProxyGet(
        string path,
        string queryString,
        CancellationToken cancellationToken = default
    )
    {
        ProxyCalls.Add(path + queryString);
        return Task.FromResult(Proxy.Dequeue());
    }

    public static CopilotResult<CopilotUserBundleDto> Mapped(string login = "user-a") =>
        new(
            new CopilotUserBundleDto
            {
                Identity = new CopilotIdentityDto { UserId = 1, Login = login },
            },
            true,
            200,
            new CopilotMetaDto { MatchedEmail = "a@dfds.com" },
            null
        );
}

public class TestCopilotInsightsApplicationService
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 23);

    private static CopilotInsightsCache NewCache() =>
        new(new MemoryCache(new MemoryCacheOptions()), NullLogger<CopilotInsightsCache>.Instance);

    private static ClaimsPrincipal User(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));

    private static ClaimsPrincipal DfdsUser() => User(("preferred_username", "a@dfds.com"));

    [Theory]
    [InlineData("a@dfds.com", true)]
    [InlineData("A@DFDS.COM", true)]
    [InlineData("a@partner.dfds.com", true)]
    [InlineData("a@dfds.onmicrosoft.com", true)]
    [InlineData("a@x.dfds.onmicrosoft.com", true)]
    [InlineData("a@notdfds.com", false)]
    [InlineData("a@dfds.com.evil.com", false)]
    [InlineData("a@gmail.com", false)]
    [InlineData("a@dfds.cloud", false)]
    [InlineData("@dfds.com", false)]
    [InlineData("dfds.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void dfds_tenant_email(string? email, bool expected)
    {
        Assert.Equal(expected, CopilotInsightsApplicationService.IsDfdsTenantEmail(email));
    }

    [Fact]
    public void caller_addresses_come_from_every_claim_normalized_and_distinct()
    {
        var principal = User(
            ("preferred_username", " Jane.Doe@DFDS.com "),
            ("upn", "jadoe@dfds.com"),
            (ClaimTypes.Upn, "jane.doe@dfds.com"),
            (ClaimTypes.Email, "jane@gmail.com"),
            ("email", "jane.doe@partner.dfds.com"),
            ("name", "someone-else@dfds.com")
        );

        var addresses = CopilotInsightsApplicationService.CallerAddresses(principal);

        Assert.Equal(new[] { "jane.doe@dfds.com", "jadoe@dfds.com", "jane.doe@partner.dfds.com" }, addresses);
    }

    [Fact]
    public async Task own_usage_sends_all_caller_addresses()
    {
        var client = new FakeCopilotInsightsClient();
        client.ByEmail.Enqueue(FakeCopilotInsightsClient.Mapped());
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var usage = await service.GetOwnUsage(
            User(("preferred_username", "a@dfds.com"), ("email", "alias@dfds.com")),
            From,
            To
        );

        Assert.True(usage.Available);
        Assert.True(usage.Mapped);
        Assert.Equal("user-a", usage.Bundle!.Identity.Login);
        Assert.Equal("a@dfds.com", usage.Meta!.MatchedEmail);
        Assert.Equal(new[] { "a@dfds.com", "alias@dfds.com" }, Assert.Single(client.ByEmailCalls));
    }

    [Fact]
    public async Task service_principal_is_rejected_without_a_lookup()
    {
        var client = new FakeCopilotInsightsClient();
        var service = new CopilotInsightsApplicationService(client, NewCache());
        var principal = User(
            ("idtyp", "app"),
            ("oid", "3f0f2f1e-5a4e-4f59-9a55-7d7d1f0b1c2d"),
            ("email", "a@dfds.com")
        );

        var usage = await service.GetOwnUsage(principal, From, To);

        Assert.True(usage.Available);
        Assert.False(usage.Mapped);
        Assert.Equal(CopilotUnmappedReason.ServicePrincipal, usage.UnmappedReason);
        Assert.Empty(client.ByEmailCalls);
    }

    [Fact]
    public async Task caller_without_a_dfds_address_is_rejected_without_a_lookup()
    {
        var client = new FakeCopilotInsightsClient();
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var usage = await service.GetOwnUsage(User(("preferred_username", "guest@contractor.com")), From, To);

        Assert.False(usage.Mapped);
        Assert.Equal(CopilotUnmappedReason.NoDfdsEmail, usage.UnmappedReason);
        Assert.Empty(client.ByEmailCalls);
    }

    [Fact]
    public async Task unmapped_caller_carries_the_upstream_reason()
    {
        var client = new FakeCopilotInsightsClient();
        client.ByEmail.Enqueue(new CopilotResult<CopilotUserBundleDto>(null, true, 404, null, "not_mapped"));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var usage = await service.GetOwnUsage(DfdsUser(), From, To);

        Assert.True(usage.Available);
        Assert.False(usage.Mapped);
        Assert.Equal(CopilotUnmappedReason.NotMapped, usage.UnmappedReason);
    }

    [Fact]
    public async Task unavailable_upstream_is_reported_not_thrown()
    {
        var client = new FakeCopilotInsightsClient();
        client.ByEmail.Enqueue(CopilotResult<CopilotUserBundleDto>.Unavailable(503));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var usage = await service.GetOwnUsage(DfdsUser(), From, To);

        Assert.False(usage.Available);
        Assert.False(usage.Mapped);
        Assert.Null(usage.UnmappedReason);
    }

    [Fact]
    public async Task own_usage_is_cached_per_caller_and_range()
    {
        var client = new FakeCopilotInsightsClient();
        client.ByEmail.Enqueue(FakeCopilotInsightsClient.Mapped("first"));
        client.ByEmail.Enqueue(FakeCopilotInsightsClient.Mapped("other-range"));
        client.ByEmail.Enqueue(FakeCopilotInsightsClient.Mapped("other-user"));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var first = await service.GetOwnUsage(DfdsUser(), From, To);
        var again = await service.GetOwnUsage(DfdsUser(), From, To);
        var otherRange = await service.GetOwnUsage(DfdsUser(), From.AddDays(1), To);
        var otherUser = await service.GetOwnUsage(User(("preferred_username", "b@dfds.com")), From, To);

        Assert.Equal("first", first.Bundle!.Identity.Login);
        Assert.Equal("first", again.Bundle!.Identity.Login);
        Assert.Equal("other-range", otherRange.Bundle!.Identity.Login);
        Assert.Equal("other-user", otherUser.Bundle!.Identity.Login);
        Assert.Equal(3, client.ByEmailCalls.Count);
    }

    [Fact]
    public async Task user_usage_is_cached_per_login_ignoring_case()
    {
        var client = new FakeCopilotInsightsClient();
        client.ByLogin.Enqueue(FakeCopilotInsightsClient.Mapped("octo-cat"));
        client.ByLogin.Enqueue(FakeCopilotInsightsClient.Mapped("other"));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var first = await service.GetUserUsage("octo-cat", From, To);
        var again = await service.GetUserUsage("Octo-Cat", From, To);
        var other = await service.GetUserUsage("other", From, To);

        Assert.True(first.Mapped);
        Assert.Equal("octo-cat", again.Bundle!.Identity.Login);
        Assert.Equal("other", other.Bundle!.Identity.Login);
        Assert.Equal(new[] { "octo-cat", "other" }, client.ByLoginCalls);
    }

    [Fact]
    public async Task unknown_login_is_unmapped_with_user_not_found()
    {
        var client = new FakeCopilotInsightsClient();
        client.ByLogin.Enqueue(new CopilotResult<CopilotUserBundleDto>(null, true, 404, null, "user_not_found"));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var usage = await service.GetUserUsage("ghost", From, To);

        Assert.True(usage.Available);
        Assert.False(usage.Mapped);
        Assert.Equal(CopilotUnmappedReason.UserNotFound, usage.UnmappedReason);
    }

    [Fact]
    public async Task user_search_is_cached_and_unavailable_is_null()
    {
        var client = new FakeCopilotInsightsClient();
        client.Search.Enqueue(
            new CopilotResult<List<CopilotIdentitySummaryDto>>(
                new List<CopilotIdentitySummaryDto> { new() { Login = "octo-cat" } },
                true,
                200,
                null,
                null
            )
        );
        client.Search.Enqueue(CopilotResult<List<CopilotIdentitySummaryDto>>.Unavailable(503));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var first = await service.SearchUsers("Octo", 10);
        var again = await service.SearchUsers("octo", 10);
        var down = await service.SearchUsers("other", 10);

        Assert.Equal("octo-cat", Assert.Single(first!).Login);
        Assert.Same(first, again);
        Assert.Null(down);
        Assert.Equal(new[] { ("Octo", 10), ("other", 10) }, client.SearchCalls);
    }

    [Fact]
    public void credit_shapes_for_non_admin_callers_have_no_net_or_discount()
    {
        var types = new[]
        {
            typeof(CopilotCreditTotalsDto),
            typeof(CopilotCreditAmountApiResource),
            typeof(CopilotCreditDayApiResource),
            typeof(CopilotCreditModelApiResource),
        };

        foreach (var type in types)
        {
            var names = type.GetProperties().Select(p => p.Name).ToList();
            Assert.DoesNotContain(names, n => n.Contains("Net") || n.Contains("Discount"));
        }
    }

    [Theory]
    [InlineData("summary", "summary")]
    [InlineData("seats/history", "seats/history")]
    [InlineData("reports/users/totals", "reports/users/totals")]
    [InlineData("users/by-email/Jane.Doe+x@dfds.com", "users/by-email/Jane.Doe%2Bx%40dfds.com")]
    [InlineData("users/octo-cat", "users/octo-cat")]
    [InlineData("identity/users", "identity/users")]
    [InlineData("budgets", "budgets")]
    public void admin_path_allowed(string path, string expected)
    {
        Assert.True(CopilotAdminPaths.TryEscape(path, out var escaped));
        Assert.Equal(expected, escaped);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin/collect/members")]
    [InlineData("healthz")]
    [InlineData("metrics/daily")]
    [InlineData("../secrets")]
    [InlineData("users/../admin/collect/members")]
    [InlineData("users/./x")]
    [InlineData("users//x")]
    [InlineData("users/x/")]
    [InlineData("users/a%2Fb")]
    [InlineData("users/a\\b")]
    [InlineData("users/a b")]
    [InlineData("users/a?b")]
    [InlineData("Summary")]
    public void admin_path_rejected(string? path)
    {
        Assert.False(CopilotAdminPaths.TryEscape(path, out _));
    }

    [Fact]
    public async Task admin_response_gets_copilot_available_and_is_cached_by_path_and_query()
    {
        var client = new FakeCopilotInsightsClient();
        client.Proxy.Enqueue(
            new CopilotProxyResponse(true, 200, """{"data":[1],"meta":{"scope":"enterprise:dfds"}}""")
        );
        client.Proxy.Enqueue(new CopilotProxyResponse(true, 200, """{"data":[2]}"""));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var first = await service.GetAdmin("summary", "?scope=enterprise:dfds");
        var cached = await service.GetAdmin("summary", "?scope=enterprise:dfds");
        var otherQuery = await service.GetAdmin("summary", "?scope=org:dfds");

        using var doc = JsonDocument.Parse(first.Body!);
        Assert.True(doc.RootElement.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
        Assert.Equal("enterprise:dfds", doc.RootElement.GetProperty("meta").GetProperty("scope").GetString());
        Assert.Equal(first.Body, cached.Body);

        using var other = JsonDocument.Parse(otherQuery.Body!);
        Assert.True(other.RootElement.GetProperty("meta").GetProperty("copilotAvailable").GetBoolean());
        Assert.Equal(new[] { "summary?scope=enterprise:dfds", "summary?scope=org:dfds" }, client.ProxyCalls);
    }

    [Fact]
    public async Task admin_error_status_passes_through_with_its_body()
    {
        var client = new FakeCopilotInsightsClient();
        client.Proxy.Enqueue(new CopilotProxyResponse(true, 400, """{"error":"scope_required","detail":"x"}"""));
        var service = new CopilotInsightsApplicationService(client, NewCache());

        var response = await service.GetAdmin("summary", "");

        Assert.True(response.Available);
        Assert.Equal(400, response.StatusCode);
        using var doc = JsonDocument.Parse(response.Body!);
        Assert.Equal("scope_required", doc.RootElement.GetProperty("error").GetString());
    }
}

public class TestCopilotInsightsCache
{
    private static CopilotInsightsCache NewCache(TimeSpan? fetchTimeout = null) =>
        new(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<CopilotInsightsCache>.Instance,
            fetchTimeout ?? TimeSpan.FromSeconds(20)
        );

    private static Func<CancellationToken, Task<string>> Returns(string value, List<string> calls) =>
        _ =>
        {
            calls.Add(value);
            return Task.FromResult(value);
        };

    private static bool IsFailure(string value) => value == "fail";

    [Fact]
    public async Task fresh_value_is_served_without_fetching()
    {
        var cache = NewCache();
        var calls = new List<string>();

        await cache.Get("k", TimeSpan.FromMinutes(1), Returns("one", calls), IsFailure);
        var second = await cache.Get("k", TimeSpan.FromMinutes(1), Returns("two", calls), IsFailure);

        Assert.Equal("one", second);
        Assert.Equal(new[] { "one" }, calls);
    }

    [Fact]
    public async Task expired_value_is_refetched()
    {
        var cache = NewCache();
        var calls = new List<string>();

        await cache.Get("k", TimeSpan.Zero, Returns("one", calls), IsFailure);
        var second = await cache.Get("k", TimeSpan.Zero, Returns("two", calls), IsFailure);

        Assert.Equal("two", second);
        Assert.Equal(new[] { "one", "two" }, calls);
    }

    [Fact]
    public async Task stale_value_is_served_when_the_refresh_fails()
    {
        var cache = NewCache();
        var calls = new List<string>();

        await cache.Get("k", TimeSpan.Zero, Returns("one", calls), IsFailure);
        var failed = await cache.Get("k", TimeSpan.Zero, Returns("fail", calls), IsFailure);
        var thrown = await cache.Get<string>(
            "k",
            TimeSpan.Zero,
            _ => throw new HttpRequestException("down"),
            IsFailure
        );

        Assert.Equal("one", failed);
        Assert.Equal("one", thrown);
    }

    [Fact]
    public async Task failure_without_a_stale_value_is_returned_and_not_cached()
    {
        var cache = NewCache();
        var calls = new List<string>();

        var failed = await cache.Get("k", TimeSpan.FromMinutes(1), Returns("fail", calls), IsFailure);
        var next = await cache.Get("k", TimeSpan.FromMinutes(1), Returns("one", calls), IsFailure);

        Assert.Equal("fail", failed);
        Assert.Equal("one", next);
        await Assert.ThrowsAsync<HttpRequestException>(
            () => cache.Get<string>("other", TimeSpan.Zero, _ => throw new HttpRequestException("down"), IsFailure)
        );
    }

    [Fact]
    public async Task concurrent_misses_fetch_independently()
    {
        var cache = NewCache();
        var release = new TaskCompletionSource();
        var fetches = 0;

        async Task<string> Fetch(CancellationToken _)
        {
            Interlocked.Increment(ref fetches);
            await release.Task;
            return "one";
        }

        var callers = Enumerable
            .Range(0, 5)
            .Select(_ => cache.Get("k", TimeSpan.FromMinutes(1), Fetch, IsFailure))
            .ToList();
        release.SetResult();
        var results = await Task.WhenAll(callers);

        Assert.All(results, r => Assert.Equal("one", r));
        Assert.Equal(5, fetches);
    }

    [Fact]
    public async Task fetch_is_cancelled_after_the_fetch_timeout()
    {
        var cache = NewCache(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () =>
                cache.Get(
                    "k",
                    TimeSpan.FromMinutes(1),
                    async ct =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), ct);
                        return "late";
                    },
                    IsFailure
                )
        );
    }
}
