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

public class CatalogApplicationService : ICatalogApplicationService
{
    private readonly CatalogSnapshotCache _snapshots;

    public CatalogApplicationService(CatalogSnapshotCache snapshots)
    {
        _snapshots = snapshots;
    }

    public async Task<CatalogResult<ApplicationEntryDto>> GetDeploymentsForCapability(
        CapabilityId capabilityId,
        CancellationToken cancellationToken = default
    )
    {
        var merged = await _snapshots.Get(cancellationToken);
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
        var merged = await _snapshots.Get(cancellationToken);
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
        var merged = await _snapshots.Get(cancellationToken);
        return new CatalogResult<NamespaceEntryDto>(merged.Namespaces, merged.Availability);
    }

    public async Task<CatalogResult<DependencyEdgeDto>> GetDependencies(
        DependencyFilters filters,
        CancellationToken cancellationToken = default
    )
    {
        var merged = await _snapshots.Get(cancellationToken);
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
}
