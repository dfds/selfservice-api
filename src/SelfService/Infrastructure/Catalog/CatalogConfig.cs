namespace SelfService.Infrastructure.Catalog;

public sealed record CatalogClusterEndpoint(string Cluster, Uri Url);

public sealed class CatalogConfig
{
    public IReadOnlyList<CatalogClusterEndpoint> Clusters { get; }
    public string Scope { get; }

    public bool TokenAcquisitionEnabled => !string.IsNullOrWhiteSpace(Scope);

    public CatalogConfig(IReadOnlyList<CatalogClusterEndpoint> clusters, string? scope)
    {
        Clusters = clusters;
        Scope = scope ?? "";
    }

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
