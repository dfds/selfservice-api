using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Persistence;

public class UserFilterService : IUserFilterService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly SelfServiceDbContext _dbContext;

    public UserFilterService(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAudienceResolution> ResolveUsers(string audienceJson)
    {
        var audience = JsonSerializer.Deserialize<UserAudienceConfig>(audienceJson, JsonOptions);
        if (audience == null)
            return new UserAudienceResolution();

        return audience.Mode switch
        {
            "all" => new UserAudienceResolution { Members = await _dbContext.Members.ToListAsync() },
            "specific" => await ResolveSpecific(audience.UserEmails ?? Array.Empty<string>()),
            "filter" => new UserAudienceResolution
            {
                Members = await ResolveFiltered(audience.Filters ?? Array.Empty<UserAudienceFilterCondition>()),
            },
            _ => new UserAudienceResolution(),
        };
    }

    private async Task<UserAudienceResolution> ResolveSpecific(string[] emails)
    {
        var normalized = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
            return new UserAudienceResolution();

        // PostgreSQL string comparisons are case-sensitive by default; lower both sides for the match.
        var lower = normalized.Select(e => e.ToLowerInvariant()).ToArray();
        var members = await _dbContext.Members.Where(m => lower.Contains(m.Email.ToLower())).ToListAsync();

        var matched = new HashSet<string>(members.Select(m => m.Email), StringComparer.OrdinalIgnoreCase);
        var unmatched = normalized.Where(e => !matched.Contains(e)).ToList();

        return new UserAudienceResolution { Members = members, UnmatchedEmails = unmatched };
    }

    private async Task<List<Member>> ResolveFiltered(UserAudienceFilterCondition[] filters)
    {
        IQueryable<Member> query = _dbContext.Members;

        foreach (var filter in filters)
        {
            query = ApplyFilter(query, filter);
        }

        return await query.ToListAsync();
    }

    private IQueryable<Member> ApplyFilter(IQueryable<Member> query, UserAudienceFilterCondition filter) =>
        filter.Field?.ToLowerInvariant() switch
        {
            "email" => ApplyEmailFilter(query, filter.Operator, filter.Value),
            "displayname" => ApplyDisplayNameFilter(query, filter.Operator, filter.Value),
            "lastseen" => ApplyLastSeenFilter(query, filter.Operator, filter.Value),
            "capabilitycostcentre" => ApplyCapabilityCostCentreFilter(query, filter.Value),
            _ => query,
        };

    private static IQueryable<Member> ApplyEmailFilter(IQueryable<Member> query, string? op, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return query;
        var lower = value.ToLowerInvariant();
        return op switch
        {
            "eq" => query.Where(m => m.Email.ToLower() == lower),
            "contains" => query.Where(m => m.Email.ToLower().Contains(lower)),
            _ => query,
        };
    }

    private static IQueryable<Member> ApplyDisplayNameFilter(IQueryable<Member> query, string? op, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return query;
        var lower = value.ToLowerInvariant();
        return op switch
        {
            "eq" => query.Where(m => m.DisplayName != null && m.DisplayName.ToLower() == lower),
            "contains" => query.Where(m => m.DisplayName != null && m.DisplayName.ToLower().Contains(lower)),
            _ => query,
        };
    }

    private static IQueryable<Member> ApplyLastSeenFilter(IQueryable<Member> query, string? op, string? value)
    {
        if (!DateTime.TryParse(value, out var date))
            return query;

        // Members with LastSeen == null fall out of every binary comparison (SQL NULL semantics),
        // matching the documented behaviour: "never seen" users are not included in any LastSeen filter.
        return op switch
        {
            "gte" => query.Where(m => m.LastSeen >= date),
            "lte" => query.Where(m => m.LastSeen <= date),
            "gt" => query.Where(m => m.LastSeen > date),
            "lt" => query.Where(m => m.LastSeen < date),
            _ => query,
        };
    }

    // Operator is ignored — only "in" (any-of) is meaningful for cost-centre membership.
    // Value is a comma-separated list of cost-centre slugs (e.g. "ti-cae,ti-platform"). Empty = no narrowing.
    private IQueryable<Member> ApplyCapabilityCostCentreFilter(IQueryable<Member> query, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return query;

        var snippets = value
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(slug => "{\"dfds.cost.centre\":\"" + slug + "\"}")
            .ToList();

        if (snippets.Count == 0)
            return query;

        return query.Where(m =>
            _dbContext.Memberships.Any(ms =>
                ms.UserId == m.Id
                && _dbContext.Capabilities.Any(c =>
                    c.Id == ms.CapabilityId
                    && c.JsonMetadata != null
                    && snippets.Any(snippet => EF.Functions.JsonContains(c.JsonMetadata, snippet))
                )
            )
        );
    }
}

internal class UserAudienceConfig
{
    public string Mode { get; set; } = "all";
    public string[]? UserEmails { get; set; }
    public UserAudienceFilterCondition[]? Filters { get; set; }
}

internal class UserAudienceFilterCondition
{
    public string? Field { get; set; }
    public string? Operator { get; set; }
    public string? Value { get; set; }
}
