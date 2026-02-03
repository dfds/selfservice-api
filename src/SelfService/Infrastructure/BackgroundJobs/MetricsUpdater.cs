using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Metrics;
using SelfService.Infrastructure.Api.System;

namespace SelfService.Infrastructure.BackgroundJobs;

public class MetricsUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsUpdater> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CapabilityMetrics _capabilityMetrics;
    public DateTime UpdateNeededAt;

    public MetricsUpdater(
        IServiceProvider serviceProvider,
        ILogger<MetricsUpdater> logger,
        IServiceScopeFactory scopeFactory
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _capabilityMetrics = new CapabilityMetrics(_scopeFactory);
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
