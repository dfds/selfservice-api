using SelfService.Infrastructure.Api.Metrics;

namespace SelfService.Infrastructure.BackgroundJobs;

public class MetricsUpdater : BackgroundService
{
    private readonly ILogger<MetricsUpdater> _logger;
    private readonly CapabilityMetrics _capabilityMetrics;
    public DateTime UpdateNeededAt;

    public MetricsUpdater(ILogger<MetricsUpdater> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _capabilityMetrics = new CapabilityMetrics(scopeFactory);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await DoWork(stoppingToken);
                    var nextRunTimestamp = UpdateNeededAt;
                    var delay = nextRunTimestamp > DateTime.Now ? nextRunTimestamp - DateTime.Now : TimeSpan.Zero;
                    await Task.Delay(delay, stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope("{BackgroundJob} {CorrelationId}", nameof(MetricsUpdater), Guid.NewGuid());

        try
        {
            await _capabilityMetrics.UpdateCapabilityCache();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating metrics");
            _logger.LogCritical(e, "Metrics update failed. Will retry in 10 minutes");
        }
        UpdateNeededAt = DateTime.Now.AddMinutes(10);
    }
}
