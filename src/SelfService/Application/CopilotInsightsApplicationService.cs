using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.CopilotInsights;

namespace SelfService.Application;

public interface ICopilotInsightsApplicationService
{
    Task<CopilotResult<CopilotUserBundleDto>> GetOwnUsage(
        ClaimsPrincipal principal,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    Task<CopilotResult<CopilotUserBundleDto>> GetUserUsage(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    // Returns null when copilot-insights could not answer.
    Task<IReadOnlyList<CopilotIdentitySummaryDto>?> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    );

    // path must already be validated and escaped by CopilotAdminPaths.TryEscape.
    Task<CopilotProxyResponse> GetAdmin(string path, string queryString, CancellationToken cancellationToken = default);
}

public class CopilotInsightsApplicationService : ICopilotInsightsApplicationService
{
    public static readonly TimeSpan OwnUsageTtl = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan AdminTtl = TimeSpan.FromMinutes(5);

    // UPN and SMTP mail can differ; GitHub may hold either address.
    private static readonly string[] AddressClaimTypes =
    {
        "preferred_username",
        "upn",
        ClaimTypes.Upn,
        ClaimTypes.Email,
        "email",
    };

    private readonly ICopilotInsightsClient _client;
    private readonly CopilotInsightsCache _cache;

    public CopilotInsightsApplicationService(ICopilotInsightsClient client, CopilotInsightsCache cache)
    {
        _client = client;
        _cache = cache;
    }

    public Task<CopilotResult<CopilotUserBundleDto>> GetOwnUsage(
        ClaimsPrincipal principal,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        var caller = principal.TryGetCallerIdentity();
        var isApp = string.Equals(principal.FindFirst("idtyp")?.Value, "app", StringComparison.OrdinalIgnoreCase);
        if (isApp || caller?.Type == MemberType.ServicePrincipal)
        {
            return Task.FromResult(Unmapped(CopilotUnmappedReason.ServicePrincipal));
        }

        var addresses = CallerAddresses(principal);
        if (caller is null || addresses.Count == 0)
        {
            return Task.FromResult(Unmapped(CopilotUnmappedReason.NoDfdsEmail));
        }

        return _cache.Get(
            $"me:{caller.Id}:{RangeKey(from, to)}",
            OwnUsageTtl,
            ct => _client.GetUserByEmail(addresses, from, to, ct),
            cancellationToken
        );
    }

    public Task<CopilotResult<CopilotUserBundleDto>> GetUserUsage(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        return _cache.Get(
            $"user:{login.ToLowerInvariant()}:{RangeKey(from, to)}",
            OwnUsageTtl,
            ct => _client.GetUserByLogin(login, from, to, ct),
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<CopilotIdentitySummaryDto>?> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _cache.Get(
            $"search:{limit}:{q.ToLowerInvariant()}",
            AdminTtl,
            ct => _client.SearchUsers(q, limit, ct),
            cancellationToken
        );
        return result.Available ? result.Data : null;
    }

    public Task<CopilotProxyResponse> GetAdmin(
        string path,
        string queryString,
        CancellationToken cancellationToken = default
    )
    {
        return _cache.Get(
            $"admin:{path}{queryString}",
            AdminTtl,
            async ct => MarkAvailable(await _client.ProxyGet(path, queryString, ct)),
            cancellationToken
        );
    }

    private static CopilotResult<CopilotUserBundleDto> Unmapped(string reason) => new(null, true, ErrorCode: reason);

    private static string RangeKey(DateOnly from, DateOnly to) => $"{from:yyyy-MM-dd}:{to:yyyy-MM-dd}";

    // Distinct DFDS addresses, normalized in claim order.
    public static IReadOnlyList<string> CallerAddresses(ClaimsPrincipal principal)
    {
        var candidates = new List<string?> { principal.Identity?.Name };
        candidates.AddRange(principal.Claims.Where(c => AddressClaimTypes.Contains(c.Type)).Select(c => c.Value));

        return candidates.Select(NormalizeEmail).Where(IsDfdsTenantEmail).Distinct().Select(e => e!).ToList();
    }

    public static string? NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant();

    // Uses the same tenant domains as copilot-insights.
    public static bool IsDfdsTenantEmail(string? email)
    {
        var at = email?.LastIndexOf('@') ?? -1;
        if (at <= 0)
        {
            return false;
        }

        var domain = email![(at + 1)..].ToLowerInvariant();
        return domain is "dfds.com" or "dfds.onmicrosoft.com"
            || domain.EndsWith(".dfds.com", StringComparison.Ordinal)
            || domain.EndsWith(".dfds.onmicrosoft.com", StringComparison.Ordinal);
    }

    // Admin responses need the same availability flag as typed responses.
    private static CopilotProxyResponse MarkAvailable(CopilotProxyResponse response)
    {
        if (!response.Available || string.IsNullOrEmpty(response.Body))
        {
            return response;
        }

        try
        {
            if (JsonNode.Parse(response.Body) is JsonObject root)
            {
                if (root["meta"] is not JsonObject meta)
                {
                    meta = new JsonObject();
                    root["meta"] = meta;
                }
                meta["copilotAvailable"] = true;
                return response with { Body = root.ToJsonString() };
            }
        }
        catch (JsonException) { }

        return response;
    }
}

public static class CopilotAdminPaths
{
    private static readonly HashSet<string> AllowedRoots = new(StringComparer.Ordinal)
    {
        "scopes",
        "status",
        "summary",
        "reports",
        "teams",
        "seats",
        "billing",
        "credits",
        "budgets",
        "users",
        "identity",
    };

    // Reject characters that could change path semantics upstream.
    private static readonly Regex Segment = new("^[A-Za-z0-9._@+~-]+$", RegexOptions.Compiled);

    // Validates an unescaped catch-all path against the root allowlist and segment restrictions,
    // then escapes each segment. Rejects empty paths and empty, "." or ".." segments.
    public static bool TryEscape(string? path, out string escaped)
    {
        escaped = "";
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var segments = path.Split('/');
        if (!AllowedRoots.Contains(segments[0]))
        {
            return false;
        }

        foreach (var segment in segments)
        {
            if (segment is "" or "." or ".." || !Segment.IsMatch(segment))
            {
                return false;
            }
        }

        escaped = string.Join("/", segments.Select(Uri.EscapeDataString));
        return true;
    }
}
