using SelfService.Application;

namespace SelfService.Infrastructure.BackgroundJobs;

public class CheckPendingCapabilityDeletions : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public CheckPendingCapabilityDeletions(IServiceProvider serviceProvider)
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
                    await Task.Delay(TimeSpan.FromHours(12), stoppingToken); // arbitrary long check-cycle.
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CheckPendingCapabilityDeletions>>();

        using var _ = logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(CheckPendingCapabilityDeletions),
            Guid.NewGuid()
        );

        var applicationService = scope.ServiceProvider.GetRequiredService<ICapabilityApplicationService>();

        logger.LogDebug("Checking all currently pending deletion requests for capabilities...");
        await applicationService.CheckPendingCapabilityDeletions();
    }
}
