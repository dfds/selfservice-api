using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Metrics;

public class AllCapabilitiesCostsCache
{
    private AllCapabilitiesCosts? _cachedCosts;
    private DateTime _lastUpdated = DateTime.MinValue;
    private readonly SemaphoreSlim _updateLock = new SemaphoreSlim(1, 1);

    public AllCapabilitiesCosts? CachedCosts => _cachedCosts;
    public DateTime LastUpdated => _lastUpdated;
    public bool HasData => _cachedCosts != null;

    public async Task UpdateCache(AllCapabilitiesCosts costs)
    {
        await _updateLock.WaitAsync();
        try
        {
            _cachedCosts = costs;
            _lastUpdated = DateTime.UtcNow;
        }
        finally
        {
            _updateLock.Release();
        }
    }

    public AllCapabilitiesCosts? GetCachedData()
    {
        return _cachedCosts;
    }
}
