using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Metrics;

namespace SelfService.Infrastructure.BackgroundJobs;

public class UpdateOutOfSyncEcrRepos : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateOutOfSyncEcrRepos> _logger;

    public UpdateOutOfSyncEcrRepos(IServiceProvider serviceProvider, ILogger<UpdateOutOfSyncEcrRepos> logger)
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
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); //maybe each minute is a lot?
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
