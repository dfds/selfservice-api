using SelfService.Application;
using SelfService.Infrastructure.Api.Metrics;

namespace SelfService.Infrastructure.BackgroundJobs;

public class AllCapabilitiesCostsCacheUpdater : BackgroundService
{
    private readonly ILogger<AllCapabilitiesCostsCacheUpdater> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AllCapabilitiesCostsCache _cache;

    // Update twice per day: 6 AM and 6 PM UTC
    private static readonly TimeSpan FirstUpdateTime = new TimeSpan(6, 0, 0);
    private static readonly TimeSpan SecondUpdateTime = new TimeSpan(18, 0, 0);

    public AllCapabilitiesCostsCacheUpdater(
        ILogger<AllCapabilitiesCostsCacheUpdater> logger,
        IServiceScopeFactory scopeFactory,
        AllCapabilitiesCostsCache cache
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(
            async () =>
            {
                // Initial update on startup
                await DoWork(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var nextRunTime = CalculateNextUpdateTime();
                    var delay = nextRunTime - DateTime.UtcNow;

                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogInformation(
                            "Next cache update scheduled for {NextUpdateTime} UTC (in {DelayMinutes} minutes)",
                            nextRunTime,
                            delay.TotalMinutes
                        );
                        await Task.Delay(delay, stoppingToken);
                    }

                    await DoWork(stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private DateTime CalculateNextUpdateTime()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Check if we should run at 6 AM today
        var firstUpdate = today.Add(FirstUpdateTime);
        if (now < firstUpdate)
        {
            return firstUpdate;
        }

        // Check if we should run at 6 PM today
        var secondUpdate = today.Add(SecondUpdateTime);
        if (now < secondUpdate)
        {
            return secondUpdate;
        }

        // Otherwise, schedule for 6 AM tomorrow
        return today.AddDays(1).Add(FirstUpdateTime);
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(AllCapabilitiesCostsCacheUpdater),
            Guid.NewGuid()
        );

        try
        {
            _logger.LogInformation("Starting cache update for all capabilities costs");

            using var serviceScope = _scopeFactory.CreateScope();
            var platformDataService = serviceScope.ServiceProvider.GetRequiredService<IPlatformDataApiRequesterService>();

            var allCapabilitiesCosts = await platformDataService.GetAllCapabilitiesCosts();
            await _cache.UpdateCache(allCapabilitiesCosts);

            _logger.LogInformation(
                "Successfully updated cache with {CostCount} capabilities costs. Last updated: {LastUpdated}",
                allCapabilitiesCosts.Costs.Count,
                _cache.LastUpdated
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating all capabilities costs cache");
            _logger.LogCritical(e, "Cache update failed. Will retry at next scheduled time");
        }
    }
}
