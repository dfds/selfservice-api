using SelfService.Domain.Services;

namespace SelfService.Infrastructure.BackgroundJobs;

public class ECRRepositoryPermissionChecker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ECRRepositoryPermissionChecker> _logger;
    private readonly bool _updateRepositoriesOnStateMismatch;

    public ECRRepositoryPermissionChecker(
        IServiceProvider serviceProvider,
        ILogger<ECRRepositoryPermissionChecker> logger
    )
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
                    // Checking every 7 days should be enough -- we don't want to hammer AWS too often.
                    await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
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
            nameof(ECRRepositoryPermissionChecker),
            Guid.NewGuid()
        );
        var ecrRepositoryService = scope.ServiceProvider.GetRequiredService<IECRRepositoryService>();

        try
        {
            //await nothing
            await ecrRepositoryService.GetAllECRRepositories(); // dummy

            //await ecrRepositoryService.CheckECRRepositoriosPermissions(_updateRepositoriesOnStateMismatch);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error checking ECR repository permissions");
        }
    }
}
