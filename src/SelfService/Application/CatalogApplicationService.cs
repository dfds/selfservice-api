using Microsoft.Extensions.Caching.Memory;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Catalog;

namespace SelfService.Application;

public sealed record CatalogAvailability(
    bool CatalogAvailable,
    int ClustersQueried,
    int ClustersFailed,
    DateTime? CollectedAt,
    DateTime? PublishedAt
);

public sealed record CatalogResult<T>(IReadOnlyList<T> Items, CatalogAvailability Availability);

public sealed record ApplicationFilters(
    string? CapabilityId = null,
    string? Namespace = null,
    string? Kind = null,
    string? Query = null,
    bool? HasDocs = null
);

public sealed record DependencyFilters(string? Namespace = null, string? Type = null);

public interface ICatalogApplicationService
{
    Task<CatalogResult<ApplicationEntryDto>> GetDeploymentsForCapability(
        CapabilityId capabilityId,
        CancellationToken cancellationToken = default
    );
    Task<CatalogResult<ApplicationEntryDto>> ListApplications(
        ApplicationFilters filters,
        CancellationToken cancellationToken = default
    );
    Task<CatalogResult<NamespaceEntryDto>> ListNamespaces(CancellationToken cancellationToken = default);
    Task<CatalogResult<DependencyEdgeDto>> GetDependencies(
        DependencyFilters filters,
        CancellationToken cancellationToken = default
    );
}

/// Caching proxy over the per-cluster ssu-catalog services. On cache miss it does a
/// full-snapshot fetch per cluster, concatenates, then once joins on capabilityId against Capability data (filtering to capability-owned apps and attaching the name). All
/// query methods read from the single cached merged structure. Only stored in-memory
public class CatalogApplicationService : ICatalogApplicationService
{
    private const string CacheKey = "catalog:merged";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(20);

    private readonly CatalogConfig _config;
    private readonly ICatalogClient _catalogClient;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CatalogApplicationService> _logger;

    public CatalogApplicationService(
        CatalogConfig config,
        ICatalogClient catalogClient,
        ICapabilityRepository capabilityRepository,
        IMemoryCache cache,
        ILogger<CatalogApplicationService> logger
    )
    {
        _config = config;
        _catalogClient = catalogClient;
        _capabilityRepository = capabilityRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CatalogResult<ApplicationEntryDto>> GetDeploymentsForCapability(
        CapabilityId capabilityId,
        CancellationToken cancellationToken = default
    )
    {
        var merged = await GetMerged();
        var id = capabilityId.ToString();
        var items = merged
            .Applications.Where(a => string.Equals(a.CapabilityId, id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new CatalogResult<ApplicationEntryDto>(items, merged.Availability);
    }

    public async Task<CatalogResult<ApplicationEntryDto>> ListApplications(
        ApplicationFilters filters,
        CancellationToken cancellationToken = default
    )
    {
        var merged = await GetMerged();
        IEnumerable<ApplicationEntryDto> apps = merged.Applications;

        if (!string.IsNullOrWhiteSpace(filters.CapabilityId))
        {
            apps = apps.Where(a =>
                string.Equals(a.CapabilityId, filters.CapabilityId, StringComparison.OrdinalIgnoreCase)
            );
        }
        if (!string.IsNullOrWhiteSpace(filters.Namespace))
        {
            apps = apps.Where(a => string.Equals(a.Namespace, filters.Namespace, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(filters.Kind))
        {
            apps = apps.Where(a => string.Equals(a.Kind, filters.Kind, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(filters.Query))
        {
            apps = apps.Where(a => a.Name.Contains(filters.Query, StringComparison.OrdinalIgnoreCase));
        }
        if (filters.HasDocs is { } hasDocs)
        {
            apps = apps.Where(a => HasDocs(a) == hasDocs);
        }

        return new CatalogResult<ApplicationEntryDto>(apps.ToList(), merged.Availability);
    }

    public async Task<CatalogResult<NamespaceEntryDto>> ListNamespaces(CancellationToken cancellationToken = default)
    {
        var merged = await GetMerged();
        return new CatalogResult<NamespaceEntryDto>(merged.Namespaces, merged.Availability);
    }

    public async Task<CatalogResult<DependencyEdgeDto>> GetDependencies(
        DependencyFilters filters,
        CancellationToken cancellationToken = default
    )
    {
        var merged = await GetMerged();
        IEnumerable<DependencyEdgeDto> deps = merged.Dependencies;

        if (!string.IsNullOrWhiteSpace(filters.Namespace))
        {
            deps = deps.Where(d =>
                string.Equals(d.Source.Namespace, filters.Namespace, StringComparison.OrdinalIgnoreCase)
                || string.Equals(d.Target.Namespace, filters.Namespace, StringComparison.OrdinalIgnoreCase)
            );
        }
        if (!string.IsNullOrWhiteSpace(filters.Type))
        {
            deps = deps.Where(d => string.Equals(d.Type, filters.Type, StringComparison.OrdinalIgnoreCase));
        }

        return new CatalogResult<DependencyEdgeDto>(deps.ToList(), merged.Availability);
    }

    private static bool HasDocs(ApplicationEntryDto app) => app.Services.Any(s => s.ApiDocs.Count > 0);

    private Task<MergedCatalog> GetMerged()
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                using var cts = new CancellationTokenSource(FetchTimeout);
                return await FetchAndMerge(cts.Token);
            }
        )!;
    }

    private async Task<MergedCatalog> FetchAndMerge(CancellationToken cancellationToken)
    {
        var registry = _config.Clusters;

        var fetches = registry
            .Select(async endpoint =>
            {
                var snapshot = await _catalogClient.GetCatalog(endpoint.Url, cancellationToken);
                return (endpoint, snapshot);
            })
            .ToList();

        var results = await Task.WhenAll(fetches);

        var apps = new List<ApplicationEntryDto>();
        var namespaces = new List<NamespaceEntryDto>();
        var dependencies = new List<DependencyEdgeDto>();
        var clustersFailed = 0;
        var collectedTimes = new List<DateTime>();
        var publishedTimes = new List<DateTime>();

        foreach (var (endpoint, snapshot) in results)
        {
            if (snapshot is null)
            {
                clustersFailed++;
                continue;
            }

            foreach (var app in snapshot.Applications)
            {
                app.Cluster = endpoint.Cluster;
            }
            foreach (var ns in snapshot.Namespaces)
            {
                ns.Cluster = endpoint.Cluster;
            }

            apps.AddRange(snapshot.Applications);
            namespaces.AddRange(snapshot.Namespaces);
            dependencies.AddRange(snapshot.Dependencies);
            if (snapshot.CollectedAt.Year > 1)
            {
                collectedTimes.Add(snapshot.CollectedAt);
            }
            if (snapshot.PublishedAt.Year > 1)
            {
                publishedTimes.Add(snapshot.PublishedAt);
            }
        }

        var availability = new CatalogAvailability(
            CatalogAvailable: registry.Count > 0 && clustersFailed < registry.Count,
            ClustersQueried: registry.Count,
            ClustersFailed: clustersFailed,
            CollectedAt: collectedTimes.Count > 0 ? collectedTimes.Min() : null,
            PublishedAt: publishedTimes.Count > 0 ? publishedTimes.Min() : null
        );

        var capabilityNames = await ResolveCapabilityNames(apps, namespaces);

        var ownedApps = apps.Where(a =>
                TryAttachCapability(a.CapabilityId, capabilityNames, name => a.CapabilityName = name)
            )
            .ToList();
        var ownedNamespaces = namespaces
            .Where(n => TryAttachCapability(n.CapabilityId, capabilityNames, name => n.CapabilityName = name))
            .ToList();

        var ownedNamespaceKeys = ownedApps
            .Select(a => (a.Cluster, a.Namespace))
            .Concat(ownedNamespaces.Select(n => (n.Cluster, n.Name)))
            .ToHashSet();
        var ownedDependencies = dependencies
            .Where(d =>
                ownedNamespaceKeys.Contains((d.Source.Cluster, d.Source.Namespace))
                || ownedNamespaceKeys.Contains((d.Target.Cluster, d.Target.Namespace))
            )
            .ToList();

        _logger.LogDebug(
            "Catalog merged: {Apps} owned apps, {Namespaces} owned namespaces, {Deps} dependencies across {Queried} clusters ({Failed} failed)",
            ownedApps.Count,
            ownedNamespaces.Count,
            ownedDependencies.Count,
            availability.ClustersQueried,
            availability.ClustersFailed
        );

        return new MergedCatalog(ownedApps, ownedNamespaces, ownedDependencies, availability);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveCapabilityNames(
        IEnumerable<ApplicationEntryDto> apps,
        IEnumerable<NamespaceEntryDto> namespaces
    )
    {
        var candidateIds = apps.Select(a => a.CapabilityId)
            .Concat(namespaces.Select(n => n.CapabilityId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var parsed = new List<CapabilityId>();
        foreach (var id in candidateIds)
        {
            if (CapabilityId.TryParse(id, out var capabilityId))
            {
                parsed.Add(capabilityId);
            }
        }

        if (parsed.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var capabilities = await _capabilityRepository.GetByIds(parsed);
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in capabilities)
        {
            names[capability.Id.ToString()] = capability.Name;
        }
        return names;
    }

    private static bool TryAttachCapability(
        string capabilityId,
        IReadOnlyDictionary<string, string> capabilityNames,
        Action<string> attachName
    )
    {
        if (string.IsNullOrWhiteSpace(capabilityId) || !capabilityNames.TryGetValue(capabilityId, out var name))
        {
            return false;
        }
        attachName(name);
        return true;
    }

    /// <summary>The cached, merged + capability-joined catalog. All query methods read from this.</summary>
    private sealed record MergedCatalog(
        IReadOnlyList<ApplicationEntryDto> Applications,
        IReadOnlyList<NamespaceEntryDto> Namespaces,
        IReadOnlyList<DependencyEdgeDto> Dependencies,
        CatalogAvailability Availability
    );
}
