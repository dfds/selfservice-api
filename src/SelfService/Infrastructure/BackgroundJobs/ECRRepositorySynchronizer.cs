using SelfService.Domain.Services;

namespace SelfService.Infrastructure.BackgroundJobs;

public class ECRRepositorySynchronizer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ECRRepositorySynchronizer> _logger;
    private readonly bool _updateRepositoriesOnStateMismatch;

    public ECRRepositorySynchronizer(IServiceProvider serviceProvider, ILogger<ECRRepositorySynchronizer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _updateRepositoriesOnStateMismatch =
            Environment.GetEnvironmentVariable("UPDATE_REPOSITORIES_ON_STATE_MISMATCH") == "true";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await DoWork(stoppingToken);
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
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
            nameof(ECRRepositorySynchronizer),
            Guid.NewGuid()
        );
        var ecrRepositoryService = scope.ServiceProvider.GetRequiredService<IECRRepositoryService>();

        try
        {
            await ecrRepositoryService.SynchronizeAwsECRAndDatabase(_updateRepositoriesOnStateMismatch);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error synchronizing ECR repositories");
        }
    }
}
