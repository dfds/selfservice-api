namespace SelfService.Infrastructure.Catalog;

/// <summary>A single per-cluster ssu-catalog endpoint from the registry.</summary>
public sealed record CatalogClusterEndpoint(string Cluster, Uri Url);

/// <summary>
/// Catalog integration configuration. The per-cluster endpoint registry comes from
/// SS_CATALOG_ENDPOINTS ("cluster=url" CSV); the single shared downstream-API scope
/// comes from SS_CATALOG_SCOPE. An empty scope (local dev) disables token acquisition,
/// so no Authorization header is sent and OIDC-disabled ssu-catalog accepts the call.
/// </summary>
public sealed class CatalogConfig
{
    public IReadOnlyList<CatalogClusterEndpoint> Clusters { get; }
    public string Scope { get; }

    /// <summary>True when a non-empty scope is configured, i.e. a bearer token must be acquired.</summary>
    public bool TokenAcquisitionEnabled => !string.IsNullOrWhiteSpace(Scope);

    public CatalogConfig(IReadOnlyList<CatalogClusterEndpoint> clusters, string? scope)
    {
        Clusters = clusters;
        Scope = scope ?? "";
    }

    /// <summary>
    /// Parses a "cluster=url,cluster2=url2" CSV into the endpoint registry. Blank and
    /// malformed entries (missing '=', empty cluster/url, non-absolute URL) are skipped.
    /// </summary>
    public static IReadOnlyList<CatalogClusterEndpoint> ParseEndpoints(string? raw)
    {
        var result = new List<CatalogClusterEndpoint>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue; // no key, skip
            }

            var cluster = part[..separator].Trim();
            var url = part[(separator + 1)..].Trim();
            if (cluster.Length == 0 || url.Length == 0)
            {
                continue;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                continue;
            }

            result.Add(new CatalogClusterEndpoint(cluster, uri));
        }

        return result;
    }
}
