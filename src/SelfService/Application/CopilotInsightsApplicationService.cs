using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.CopilotInsights;

namespace SelfService.Application;

public static class CopilotUnmappedReason
{
    public const string ServicePrincipal = "service_principal";
    public const string NoDfdsEmail = "no_dfds_email";
    public const string NotMapped = "not_mapped";
    public const string UserNotFound = "user_not_found";
}

/// <summary>
/// One user's Copilot usage: the caller's own, or a login's for read-all holders.
/// <c>Available=false</c>: copilot-insights has no data. <c>Mapped=false</c>: it has data but
/// there is no GitHub login linked to the caller, or the login is unknown.
/// </summary>
public sealed record CopilotOwnUsage(
    bool Available,
    bool Mapped,
    string? UnmappedReason,
    CopilotUserBundleDto? Bundle,
    CopilotMetaDto? Meta
);

public interface ICopilotInsightsApplicationService
{
    Task<CopilotOwnUsage> GetOwnUsage(
        ClaimsPrincipal principal,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    Task<CopilotOwnUsage> GetUserUsage(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    );

    /// Returns null when copilot-insights could not answer.
    Task<IReadOnlyList<CopilotIdentitySummaryDto>?> SearchUsers(
        string q,
        int limit,
        CancellationToken cancellationToken = default
    );

    /// <param name="path">A path accepted by <see cref="CopilotAdminPaths.TryEscape"/>, escaped.</param>
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

    public async Task<CopilotOwnUsage> GetOwnUsage(
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
            return new CopilotOwnUsage(true, false, CopilotUnmappedReason.ServicePrincipal, null, null);
        }

        var addresses = CallerAddresses(principal);
        if (caller is null || addresses.Count == 0)
        {
            return new CopilotOwnUsage(true, false, CopilotUnmappedReason.NoDfdsEmail, null, null);
        }

        var key = $"me:{caller.Id}:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}";
        var result = await _cache.Get(
            key,
            OwnUsageTtl,
            ct => _client.GetUserByEmail(addresses, from, to, ct),
            r => !r.Available,
            cancellationToken
        );
        return ToUsage(result);
    }

    public async Task<CopilotOwnUsage> GetUserUsage(
        string login,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _cache.Get(
            $"user:{login.ToLowerInvariant()}:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}",
            OwnUsageTtl,
            ct => _client.GetUserByLogin(login, from, to, ct),
            r => !r.Available,
            cancellationToken
        );
        return ToUsage(result);
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
            r => !r.Available,
            cancellationToken
        );
        return result.Available ? result.Data : null;
    }

    private static CopilotOwnUsage ToUsage(CopilotResult<CopilotUserBundleDto> result)
    {
        if (!result.Available)
        {
            return new CopilotOwnUsage(false, false, null, null, null);
        }
        if (result.Data is null)
        {
            return new CopilotOwnUsage(true, false, result.ErrorCode ?? CopilotUnmappedReason.NotMapped, null, null);
        }
        return new CopilotOwnUsage(true, true, null, result.Data, result.Meta);
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
            r => !r.Available,
            cancellationToken
        );
    }

    /// Distinct DFDS addresses, normalized in claim order.
    public static IReadOnlyList<string> CallerAddresses(ClaimsPrincipal principal)
    {
        var candidates = new List<string?> { principal.Identity?.Name };
        candidates.AddRange(principal.Claims.Where(c => AddressClaimTypes.Contains(c.Type)).Select(c => c.Value));

        return candidates.Select(NormalizeEmail).Where(IsDfdsTenantEmail).Distinct().Select(e => e!).ToList();
    }

    public static string? NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant();

    /// Accepts dfds.com, dfds.onmicrosoft.com, and their subdomains, matching copilot-insights.
    public static bool IsDfdsTenantEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var at = email.LastIndexOf('@');
        if (at <= 0)
        {
            return false;
        }

        var domain = email[(at + 1)..].Trim().ToLowerInvariant();
        return domain == "dfds.com"
            || domain.EndsWith(".dfds.com", StringComparison.Ordinal)
            || domain == "dfds.onmicrosoft.com"
            || domain.EndsWith(".dfds.onmicrosoft.com", StringComparison.Ordinal);
    }

    /// Sets <c>meta.copilotAvailable=true</c> on a JSON object body, so every copilot response
    /// carries the flag. Other bodies pass through unchanged.
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
        catch (JsonException)
        {
        }

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

    /// <summary>
    /// Accepts a catch-all route value such as <c>users/by-email/a@dfds.com</c>. Rejects an empty
    /// path, empty, "." or ".." segments, characters outside <see cref="Segment"/>, and roots
    /// outside the allowlist. The result is escaped segment by segment.
    /// </summary>
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
