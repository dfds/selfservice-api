using Microsoft.Extensions.Caching.Memory;

namespace SelfService.Application;

/// <summary>
/// Read-through cache for copilot-insights responses. A value is fresh for its TTL and kept
/// for <see cref="StaleFor"/> after that, so a failed refresh can serve the previous value
/// instead of an unavailable response.
/// </summary>
public sealed class CopilotInsightsCache
{
    public static readonly TimeSpan StaleFor = TimeSpan.FromHours(1);
    private static readonly TimeSpan DefaultFetchTimeout = TimeSpan.FromSeconds(20);

    private readonly IMemoryCache _cache;
    private readonly ILogger<CopilotInsightsCache> _logger;
    private readonly TimeSpan _fetchTimeout;

    public CopilotInsightsCache(IMemoryCache cache, ILogger<CopilotInsightsCache> logger)
        : this(cache, logger, DefaultFetchTimeout) { }

    public CopilotInsightsCache(IMemoryCache cache, ILogger<CopilotInsightsCache> logger, TimeSpan fetchTimeout)
    {
        _cache = cache;
        _logger = logger;
        _fetchTimeout = fetchTimeout;
    }

    /// <param name="isFailure">
    /// True for a result that must not replace a cached value. A stale value is returned instead
    /// when one exists; otherwise the failed result is returned uncached.
    /// </param>
    public async Task<T> Get<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> fetch,
        Func<T, bool> isFailure,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = "copilot-insights:" + key;
        if (TryGetFresh(cacheKey, out T fresh))
        {
            return fresh;
        }

        cancellationToken.ThrowIfCancellationRequested();

        T result;
        try
        {
            using var cts = new CancellationTokenSource(_fetchTimeout);
            result = await fetch(cts.Token);
        }
        catch (Exception e)
        {
            if (TryGetStale(cacheKey, out T staleOnError))
            {
                _logger.LogWarning(e, "copilot-insights fetch failed; serving the stale value");
                return staleOnError;
            }
            throw;
        }

        if (isFailure(result))
        {
            return TryGetStale(cacheKey, out T stale) ? stale : result;
        }

        _cache.Set(cacheKey, new Entry(result!, DateTime.UtcNow + ttl), ttl + StaleFor);
        return result;
    }

    private bool TryGetFresh<T>(string cacheKey, out T value)
    {
        if (_cache.TryGetValue(cacheKey, out Entry? entry) && entry!.FreshUntil > DateTime.UtcNow)
        {
            value = (T)entry.Value;
            return true;
        }
        value = default!;
        return false;
    }

    private bool TryGetStale<T>(string cacheKey, out T value)
    {
        if (_cache.TryGetValue(cacheKey, out Entry? entry))
        {
            value = (T)entry!.Value;
            return true;
        }
        value = default!;
        return false;
    }

    private sealed record Entry(object Value, DateTime FreshUntil);
}
