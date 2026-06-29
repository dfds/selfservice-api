using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Metrics;

namespace SelfService.Infrastructure.Persistence;

public class CapabilityFilterService : ICapabilityFilterService
{
    // "Monthly Cost (USD)" — sum of the last 30 daily cost datapoints, matching the
    // portal's "Cost (last 30 days)" column / capability cost page.
    private const int CostWindowDays = 30;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly SelfServiceDbContext _dbContext;
    private readonly AllCapabilitiesCostsCache _costsCache;

    public CapabilityFilterService(SelfServiceDbContext dbContext, AllCapabilitiesCostsCache costsCache)
    {
        _dbContext = dbContext;
        _costsCache = costsCache;
    }

    public async Task<List<Capability>> ResolveCapabilities(string audienceJson)
    {
        var audience = JsonSerializer.Deserialize<AudienceConfig>(audienceJson, JsonOptions);

        if (audience == null)
            return new List<Capability>();

        return audience.Mode switch
        {
            "all" => await _dbContext.Capabilities.Where(c => c.Status == CapabilityStatusOptions.Active).ToListAsync(),
            "specific" => await ResolveSpecific(audience.CapabilityIds ?? Array.Empty<string>()),
            "filter" => await ResolveFiltered(audience.Filters ?? Array.Empty<AudienceFilterCondition>()),
            _ => new List<Capability>(),
        };
    }

    private async Task<List<Capability>> ResolveSpecific(string[] capabilityIds)
    {
        var parsedIds = new List<CapabilityId>();
        foreach (var idStr in capabilityIds)
        {
            if (CapabilityId.TryParse(idStr, out var capId))
                parsedIds.Add(capId);
        }

        if (parsedIds.Count == 0)
            return new List<Capability>();

        return await _dbContext.Capabilities.Where(c => parsedIds.Contains(c.Id)).ToListAsync();
    }

    private async Task<List<Capability>> ResolveFiltered(AudienceFilterCondition[] filters)
    {
        IQueryable<Capability> query = _dbContext.Capabilities.Where(c => c.Status != CapabilityStatusOptions.Deleted);

        foreach (var filter in filters)
        {
            query = ApplyFilter(query, filter);
        }

        var capabilities = await query.ToListAsync();

        // Cost filters can't be translated to SQL — cost data lives in the in-memory
        // AllCapabilitiesCostsCache (refreshed twice daily from platform-data-api), not the
        // database — so apply them in-memory after the DB-translatable filters have run.
        var costFilters = filters.Where(f => f.Field?.ToLowerInvariant() == "cost").ToArray();
        if (costFilters.Length > 0)
            capabilities = ApplyCostFilters(capabilities, costFilters);

        return capabilities;
    }

    private List<Capability> ApplyCostFilters(List<Capability> capabilities, AudienceFilterCondition[] costFilters)
    {
        var costsById =
            _costsCache
                .GetCachedData()
                ?.Costs.GroupBy(c => c.CapabilityId)
                .ToDictionary(g => g.Key, g => g.First()) ?? new Dictionary<string, CapabilityCosts>();

        foreach (var filter in costFilters)
        {
            if (!float.TryParse(filter.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
                continue;

            var op = filter.Operator;
            capabilities = capabilities.Where(c => MatchesCost(costsById, c, op, threshold)).ToList();
        }

        return capabilities;
    }

    private static bool MatchesCost(
        IReadOnlyDictionary<string, CapabilityCosts> costsById,
        Capability capability,
        string? op,
        float threshold
    )
    {
        var cost = costsById.GetValueOrDefault(capability.Id.ToString())?.SumForLastDays(CostWindowDays);

        // A capability with no cost data can't satisfy a numeric cost comparison, so exclude it.
        // This also means an empty/unpopulated cache narrows a cost-filtered audience to nothing.
        if (cost is null)
            return false;

        return op switch
        {
            "eq" => cost == threshold,
            "gte" => cost >= threshold,
            "lte" => cost <= threshold,
            "gt" => cost > threshold,
            "lt" => cost < threshold,
            _ => true,
        };
    }

    private IQueryable<Capability> ApplyFilter(IQueryable<Capability> query, AudienceFilterCondition filter)
    {
        return filter.Field?.ToLowerInvariant() switch
        {
            "status" => ApplyStatusFilter(query, filter.Operator, filter.Value),
            "name" => ApplyNameFilter(query, filter.Operator, filter.Value),
            "createdat" => ApplyDateFilter(query, filter.Operator, filter.Value),
            "requirementscore" => ApplyScoreFilter(query, filter.Operator, filter.Value),
            "membercount" => ApplyMemberCountFilter(query, filter.Operator, filter.Value),
            "metadatakeyexists" => ApplyMetadataKeyExistsFilter(query, filter.Value),
            "metadatakeyvalue" => ApplyMetadataKeyValueFilter(query, filter.Key, filter.Value),
            "awsaccountcount" => ApplyAwsAccountCountFilter(query, filter.Operator, filter.Value),
            "azureresourcegroupcount" => ApplyAzureResourceCountFilter(query, filter.Operator, filter.Value),
            "activemembershipapplicationcount" => ApplyActiveMembershipApplicationCountFilter(
                query,
                filter.Operator,
                filter.Value
            ),
            _ => query,
        };
    }

    private static IQueryable<Capability> ApplyStatusFilter(IQueryable<Capability> query, string? op, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return query;

        // Use explicit static constant comparisons so EF Core can translate them
        // via the registered value converter without needing to parameterise a local variable.
        return (op, value.Trim().ToLowerInvariant()) switch
        {
            ("eq", "active") => query.Where(c => c.Status == CapabilityStatusOptions.Active),
            ("eq", "pending deletion") => query.Where(c => c.Status == CapabilityStatusOptions.PendingDeletion),
            ("eq", "deleted") => query.Where(c => c.Status == CapabilityStatusOptions.Deleted),
            ("neq", "active") => query.Where(c => c.Status != CapabilityStatusOptions.Active),
            ("neq", "pending deletion") => query.Where(c => c.Status != CapabilityStatusOptions.PendingDeletion),
            ("neq", "deleted") => query.Where(c => c.Status != CapabilityStatusOptions.Deleted),
            _ => query,
        };
    }

    private static IQueryable<Capability> ApplyNameFilter(IQueryable<Capability> query, string? op, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return query;

        return op switch
        {
            "eq" => query.Where(c => c.Name == value),
            "contains" => query.Where(c => c.Name.ToLower().Contains(value.ToLower())),
            _ => query,
        };
    }

    private static IQueryable<Capability> ApplyDateFilter(IQueryable<Capability> query, string? op, string? value)
    {
        if (!DateTime.TryParse(value, out var date))
            return query;

        return op switch
        {
            "gte" => query.Where(c => c.CreatedAt >= date),
            "lte" => query.Where(c => c.CreatedAt <= date),
            "gt" => query.Where(c => c.CreatedAt > date),
            "lt" => query.Where(c => c.CreatedAt < date),
            _ => query,
        };
    }

    private static IQueryable<Capability> ApplyScoreFilter(IQueryable<Capability> query, string? op, string? value)
    {
        if (!double.TryParse(value, out var score))
            return query;

        return op switch
        {
            "eq" => query.Where(c => c.RequirementScore != null && c.RequirementScore == score),
            "gte" => query.Where(c => c.RequirementScore != null && c.RequirementScore >= score),
            "lte" => query.Where(c => c.RequirementScore != null && c.RequirementScore <= score),
            "gt" => query.Where(c => c.RequirementScore != null && c.RequirementScore > score),
            "lt" => query.Where(c => c.RequirementScore != null && c.RequirementScore < score),
            _ => query,
        };
    }

    private IQueryable<Capability> ApplyMemberCountFilter(IQueryable<Capability> query, string? op, string? value)
    {
        if (!int.TryParse(value, out var count))
            return query;

        var counts = _dbContext
            .Memberships.GroupBy(m => m.CapabilityId)
            .Select(g => new CapabilityCount { CapabilityId = g.Key, Count = g.Count() });

        return ApplyCountFilter(query, counts, op, count);
    }

    private static IQueryable<Capability> ApplyMetadataKeyExistsFilter(IQueryable<Capability> query, string? key)
    {
        if (string.IsNullOrEmpty(key))
            return query;
        var keyPattern = "\"" + key + "\"";
        return query.Where(c => c.JsonMetadata != null && c.JsonMetadata.Contains(keyPattern));
    }

    private static IQueryable<Capability> ApplyMetadataKeyValueFilter(
        IQueryable<Capability> query,
        string? key,
        string? value
    )
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            return query;
        // Use jsonb @> operator instead of LIKE — LIKE on a jsonb column is invalid in PostgreSQL.
        // @> checks structural containment, so {"key":"value"} matches regardless of JSON spacing.
        var jsonSnippet = "{\"" + key + "\":\"" + value + "\"}";
        return query.Where(c => c.JsonMetadata != null && EF.Functions.JsonContains(c.JsonMetadata, jsonSnippet));
    }

    private IQueryable<Capability> ApplyAwsAccountCountFilter(IQueryable<Capability> query, string? op, string? value)
    {
        if (!int.TryParse(value, out var count))
            return query;

        var counts = _dbContext
            .AwsAccounts.GroupBy(a => a.CapabilityId)
            .Select(g => new CapabilityCount { CapabilityId = g.Key, Count = g.Count() });

        return ApplyCountFilter(query, counts, op, count);
    }

    private IQueryable<Capability> ApplyAzureResourceCountFilter(
        IQueryable<Capability> query,
        string? op,
        string? value
    )
    {
        if (!int.TryParse(value, out var count))
            return query;

        var counts = _dbContext
            .AzureResources.GroupBy(a => a.CapabilityId)
            .Select(g => new CapabilityCount { CapabilityId = g.Key, Count = g.Count() });

        return ApplyCountFilter(query, counts, op, count);
    }

    private IQueryable<Capability> ApplyActiveMembershipApplicationCountFilter(
        IQueryable<Capability> query,
        string? op,
        string? value
    )
    {
        if (!int.TryParse(value, out var count))
            return query;

        var counts = _dbContext
            .MembershipApplications.Where(a => a.Status == MembershipApplicationStatusOptions.PendingApprovals)
            .GroupBy(a => a.CapabilityId)
            .Select(g => new CapabilityCount { CapabilityId = g.Key, Count = g.Count() });

        return ApplyCountFilter(query, counts, op, count);
    }

    private static IQueryable<Capability> ApplyCountFilter(
        IQueryable<Capability> query,
        IQueryable<CapabilityCount> counts,
        string? op,
        int count
    ) =>
        op switch
        {
            "eq" => query.Where(c => counts.Any(m => m.CapabilityId == c.Id && m.Count == count)),
            "gte" => query.Where(c => counts.Any(m => m.CapabilityId == c.Id && m.Count >= count)),
            "lte" => query.Where(c => counts.Any(m => m.CapabilityId == c.Id && m.Count <= count)),
            "gt" => query.Where(c => counts.Any(m => m.CapabilityId == c.Id && m.Count > count)),
            "lt" => query.Where(c => counts.Any(m => m.CapabilityId == c.Id && m.Count < count)),
            _ => query,
        };
}

internal class CapabilityCount
{
    public CapabilityId CapabilityId { get; set; } = null!;
    public int Count { get; set; }
}

internal class AudienceConfig
{
    public string Mode { get; set; } = "all";
    public string[]? CapabilityIds { get; set; }
    public AudienceFilterCondition[]? Filters { get; set; }
}

internal class AudienceFilterCondition
{
    public string? Field { get; set; }
    public string? Operator { get; set; }
    public string? Value { get; set; }
    public string? Key { get; set; }
}
