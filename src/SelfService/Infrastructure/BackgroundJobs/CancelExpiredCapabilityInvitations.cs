using SelfService.Application;

namespace SelfService.Infrastructure.BackgroundJobs;

public class CancelExpiredCapabilityInvitations : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public CancelExpiredCapabilityInvitations(IServiceProvider serviceProvider)
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
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CancelExpiredCapabilityInvitations>>();

        using var _ = logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(CancelExpiredCapabilityInvitations),
            Guid.NewGuid()
        );
        var applicationService = scope.ServiceProvider.GetRequiredService<IInvitationApplicationService>();

        logger.LogDebug("Cancelling any expired membership applications...");
        await applicationService.CancelExpiredCapabilityInvitations();
    }
}
