using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.BackgroundJobs;

/// <summary>
/// Rewrites today's (UTC) compliance snapshot every hour, so a past day keeps its last capture before
/// midnight. Prunes snapshots past <see cref="ComplianceRetention"/> on the first run of each UTC day.
/// </summary>
public class ComplianceSnapshotRecorder : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private DateOnly? _lastPrunedOn;

    public ComplianceSnapshotRecorder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await DoWork();
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork()
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ComplianceSnapshotRecorder>>();

        using var _ = logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(ComplianceSnapshotRecorder),
            Guid.NewGuid()
        );

        var complianceService = scope.ServiceProvider.GetRequiredService<IComplianceApplicationService>();
        var snapshotRepository = scope.ServiceProvider.GetRequiredService<IComplianceSnapshotRepository>();

        try
        {
            var capture = await complianceService.CaptureCurrent();
            await snapshotRepository.ReplaceForDate(capture.Date, capture.CapturedAt, capture.Rows, capture.Summary);

            logger.LogInformation(
                "Recorded compliance snapshot for {SnapshotDate} with {RowCount} capabilities",
                capture.Date.ToString("yyyy-MM-dd"),
                capture.Rows.Count
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error recording compliance snapshot");
        }

        try
        {
            var today = DateOnly.FromDateTime(scope.ServiceProvider.GetRequiredService<SystemTime>().Now);
            if (_lastPrunedOn == today)
            {
                return;
            }

            var cutoff = ComplianceRetention.OldestRetained(today);
            await snapshotRepository.DeleteOlderThan(cutoff);
            _lastPrunedOn = today;

            logger.LogInformation("Pruned compliance snapshots older than {Cutoff}", cutoff.ToString("yyyy-MM-dd"));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error pruning compliance snapshots");
        }
    }
}
