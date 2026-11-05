using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.BackgroundJobs;

public class UpdateOutOfSyncEcrRepos : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateOutOfSyncEcrRepos> _logger;
    private OutOfSyncECRInfo _outOfSyncEcrInfo;

    public UpdateOutOfSyncEcrRepos(
        IServiceProvider serviceProvider,
        ILogger<UpdateOutOfSyncEcrRepos> logger,
        OutOfSyncECRInfo outOfSyncEcrInfo
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _outOfSyncEcrInfo = outOfSyncEcrInfo;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await DoWork(stoppingToken);
                    var nextRunTimestamp = _outOfSyncEcrInfo.UpdateNeededAt;
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

        using var _ = _logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(UpdateOutOfSyncEcrRepos),
            Guid.NewGuid()
        );
        var ecrRepositoryService = scope.ServiceProvider.GetRequiredService<IECRRepositoryService>();

        try
        {
            await ecrRepositoryService.UpdateOutOfSyncECRInfo();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error checking out of sync ECR repositories");
        }
    }
}
