using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.BackgroundJobs;

public class RequirementScoreUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RequirementScoreUpdater(IServiceProvider serviceProvider)
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
                    await DoWork(stoppingToken);
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RequirementScoreUpdater>>();

        using var _ = logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(RequirementScoreUpdater),
            Guid.NewGuid()
        );

        var capabilityRepository = scope.ServiceProvider.GetRequiredService<ICapabilityRepository>();
        var metricsService = scope.ServiceProvider.GetRequiredService<IRequirementsMetricService>();

        try
        {
            // One query to requirements DB — all scores keyed by capabilityRootId
            var allScores = await metricsService.GetAllRequirementScoresAsync();

            // One query to main DB — all active capabilities
            var allCapabilities = await capabilityRepository.GetAll();
            var active = allCapabilities.Where(c => c.Status == CapabilityStatusOptions.Active).ToList();

            logger.LogInformation("Checking requirement scores for {Count} active capabilities", active.Count);

            // In-memory: determine which scores changed
            // Capabilities not present in allScores default to 100 (same as GetRequirementScoreAsync)
            var updates = active
                .Select(c => (capability: c, newScore: allScores.TryGetValue(c.Id.ToString(), out var s) ? s : 100.0))
                .Where(x =>
                    !x.capability.RequirementScore.HasValue
                    || Math.Abs(x.capability.RequirementScore.Value - x.newScore) >= 0.001
                )
                .ToDictionary(x => x.capability.Id, x => x.newScore);

            logger.LogInformation(
                "{UpdateCount} of {TotalCount} capabilities need score update",
                updates.Count,
                active.Count
            );

            // One bulk write — single SaveChangesAsync
            await capabilityRepository.BulkUpdateRequirementScores(updates);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating requirement scores");
        }
    }
}
