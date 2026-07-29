namespace SelfService.Application;

public sealed class CatalogSnapshotCache
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CatalogSnapshotCache> _logger;
    private readonly TimeSpan _ttl;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private volatile Entry? _entry;

    public CatalogSnapshotCache(IServiceScopeFactory scopeFactory, ILogger<CatalogSnapshotCache> logger)
        : this(scopeFactory, logger, DefaultTtl) { }

    /// TTL overload — for tests that need to age a snapshot out without waiting.
    public CatalogSnapshotCache(IServiceScopeFactory scopeFactory, ILogger<CatalogSnapshotCache> logger, TimeSpan ttl)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _ttl = ttl;
    }

    public async Task<MergedCatalog> Get(CancellationToken cancellationToken = default)
    {
        var entry = _entry;

        // Cold start — nothing to serve, so this one caller has to wait.
        if (entry is null)
        {
            return (await RefreshAndWait(cancellationToken)).Catalog;
        }

        if (!IsFresh(entry))
        {
            RefreshInBackground();
        }

        return entry.Catalog;
    }

    private async Task<Entry> RefreshAndWait(CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have populated the cache while we queued on the gate.
            var existing = _entry;
            if (existing is not null && IsFresh(existing))
            {
                return existing;
            }
            return await Fetch();
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private void RefreshInBackground()
    {
        // A refresh is already in flight — the stale snapshot stays in place until
        // it finishes. Nothing to do but let this request read the old data.
        if (!_refreshGate.Wait(0))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Fetch();
            }
            catch (Exception e)
            {
                // The previous snapshot is still there and still being served, so a
                // failed refresh degrades to stale data rather than a failed request.
                _logger.LogWarning(
                    e,
                    "Background refresh of the catalog snapshot failed; continuing to serve stale data"
                );
            }
            finally
            {
                _refreshGate.Release();
            }
        });
    }

    /// Always called with <see cref="_refreshGate"/> held.
    private async Task<Entry> Fetch()
    {
        using var scope = _scopeFactory.CreateScope();
        var fetcher = scope.ServiceProvider.GetRequiredService<ICatalogFetcher>();

        // Not tied to the triggering request: a background refresh outlives it, and a
        // cold-start caller that gives up should not abort the fetch for everyone else.
        using var cts = new CancellationTokenSource(FetchTimeout);

        var entry = new Entry(await fetcher.FetchAndMerge(cts.Token), DateTime.UtcNow);
        _entry = entry;
        return entry;
    }

    private bool IsFresh(Entry entry) => DateTime.UtcNow - entry.RefreshedAt < _ttl;

    private sealed record Entry(MergedCatalog Catalog, DateTime RefreshedAt);
}
