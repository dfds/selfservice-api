using SelfService.Domain.Services;

namespace SelfService.Infrastructure.BackgroundJobs;

public class MetricsUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsUpdater> _logger;

    public DateTime UpdateNeededAt;

    public MetricsUpdater(IServiceProvider serviceProvider, ILogger<MetricsUpdater> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        using var scope = _serviceProvider.CreateScope();

        using var _ = _logger.BeginScope("{BackgroundJob} {CorrelationId}", nameof(MetricsUpdater), Guid.NewGuid());
        var metricsService = scope.ServiceProvider.GetRequiredService<MetricsService>();

        try
        {
            await metricsService.UpdateMetrics();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating metrics");
            _logger.LogCritical(e, "Metrics update failed. Will retry in 10 minutes.");
        }
        UpdateNeededAt = DateTime.Now.AddMinutes(10);
    }
}
