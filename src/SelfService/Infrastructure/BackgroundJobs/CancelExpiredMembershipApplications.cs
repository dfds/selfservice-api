using SelfService.Application;

namespace SelfService.Infrastructure.BackgroundJobs;

public class CancelExpiredMembershipApplications : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public CancelExpiredMembershipApplications(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            await DoWork(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }, stoppingToken);
    }
    
    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CancelExpiredMembershipApplications>>();

        using var _ = logger.BeginScope("{BackgroundJob} {CorrelationId}",
            nameof(CancelExpiredMembershipApplications), Guid.NewGuid());
        
        var applicationService = scope.ServiceProvider.GetRequiredService<IMembershipApplicationService>();
        
        logger.LogDebug("Cancelling any expired membership applications...");
        await applicationService.CancelExpiredMembershipApplications();
    }
}