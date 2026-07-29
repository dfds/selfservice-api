using SelfService.Domain.Models;
using SelfService.Infrastructure.Catalog;

namespace SelfService.Application;

/// <summary>The merged, capability-joined catalog. All catalog query methods read from this.</summary>
public sealed record MergedCatalog(
    IReadOnlyList<ApplicationEntryDto> Applications,
    IReadOnlyList<NamespaceEntryDto> Namespaces,
    IReadOnlyList<DependencyEdgeDto> Dependencies,
    CatalogAvailability Availability
);

public interface ICatalogFetcher
{
    Task<MergedCatalog> FetchAndMerge(CancellationToken cancellationToken);
}

public class CatalogFetcher : ICatalogFetcher
{
    private readonly CatalogConfig _config;
    private readonly ICatalogClient _catalogClient;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly ILogger<CatalogFetcher> _logger;

    public CatalogFetcher(
        CatalogConfig config,
        ICatalogClient catalogClient,
        ICapabilityRepository capabilityRepository,
        ILogger<CatalogFetcher> logger
    )
    {
        _config = config;
        _catalogClient = catalogClient;
        _capabilityRepository = capabilityRepository;
        _logger = logger;
    }

    public async Task<MergedCatalog> FetchAndMerge(CancellationToken cancellationToken)
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
}
