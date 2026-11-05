using Microsoft.Extensions.Caching.Memory;
using SelfService.Infrastructure.CopilotInsights;

namespace SelfService.Application;

// Retains expired responses for failed refreshes, with a size limit for caller-chosen keys.
// Concurrent misses share a fetch with its own timeout; caller cancellation only stops waiting.
public sealed class CopilotInsightsCache : IDisposable
{
    public static readonly TimeSpan StaleFor = TimeSpan.FromHours(1);
    public const long DefaultSizeLimit = 64 * 1024 * 1024;
    private static readonly TimeSpan DefaultFetchTimeout = TimeSpan.FromSeconds(20);

    private readonly MemoryCache _cache;
    private readonly ILogger<CopilotInsightsCache> _logger;
    private readonly TimeSpan _fetchTimeout;
    private readonly Dictionary<string, Task<object>> _flights = new();

    public CopilotInsightsCache(ILogger<CopilotInsightsCache> logger)
        : this(logger, DefaultFetchTimeout, DefaultSizeLimit) { }

    public CopilotInsightsCache(ILogger<CopilotInsightsCache> logger, TimeSpan fetchTimeout, long sizeLimit)
    {
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit, CompactionPercentage = 0.25 });
        _logger = logger;
        _fetchTimeout = fetchTimeout;
    }

    // Unavailable results stay uncached; return a stale value when one exists.
    public async Task<T> Get<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken cancellationToken = default
    )
        where T : ICopilotResponse
    {
        if (TryGet(key, out var entry) && entry.FreshUntil > DateTime.UtcNow)
        {
            return (T)entry.Value;
        }

        cancellationToken.ThrowIfCancellationRequested();

        Task<object> flight;
        lock (_flights)
        {
            if (!_flights.TryGetValue(key, out flight!))
            {
                // Inline completion could remove the key before registration under this re-entrant lock.
                // Running on the pool makes cleanup wait for registration to release the lock.
                flight = Task.Run(() => FetchAndStore(key, ttl, fetch));
                _flights[key] = flight;
            }
        }

        return (T)await flight.WaitAsync(cancellationToken);
    }

    public void Dispose() => _cache.Dispose();

    private async Task<object> FetchAndStore<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> fetch)
        where T : ICopilotResponse
    {
        try
        {
            using var timeout = new CancellationTokenSource(_fetchTimeout);
            T result;
            try
            {
                result = await fetch(timeout.Token);
            }
            catch (Exception e) when (TryGet(key, out var staleOnError))
            {
                _logger.LogWarning(e, "copilot-insights fetch failed; serving the stale value");
                return staleOnError.Value;
            }

            if (!result.Available)
            {
                return TryGet(key, out var stale) ? stale.Value : result;
            }

            _cache.Set(
                key,
                new Entry(result, DateTime.UtcNow + ttl),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl + StaleFor,
                    Size = Math.Max(result.Size, 1),
                }
            );
            return result;
        }
        finally
        {
            // The key cannot be reused until this fetch removes it.
            lock (_flights)
            {
                _flights.Remove(key);
            }
        }
    }

    private bool TryGet(string key, out Entry entry) => _cache.TryGetValue(key, out entry!);

    private sealed record Entry(object Value, DateTime FreshUntil);
}
