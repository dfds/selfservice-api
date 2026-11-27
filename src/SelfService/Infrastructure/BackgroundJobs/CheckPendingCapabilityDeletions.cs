using SelfService.Application;

namespace SelfService.Infrastructure.BackgroundJobs;

public class ActOnPendingCapabilityDeletions : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public ActOnPendingCapabilityDeletions(IServiceProvider serviceProvider)
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
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ActOnPendingCapabilityDeletions>>();

        using var _ = logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(ActOnPendingCapabilityDeletions),
            Guid.NewGuid()
        );

        var applicationService = scope.ServiceProvider.GetRequiredService<ICapabilityApplicationService>();

        logger.LogDebug("Checking all currently pending deletion requests for capabilities...");
        try
        {
            await applicationService.ActOnPendingCapabilityDeletions();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error acting on pending capability deletions");
        }
    }
}
