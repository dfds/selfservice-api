using SelfService.Application;

namespace SelfService.Infrastructure.BackgroundJobs;

public class RemoveDeactivatedMemberships : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RemoveDeactivatedMemberships> _logger;
    private readonly IConfiguration _configuration;

    public RemoveDeactivatedMemberships(IServiceProvider serviceProvider, ILogger<RemoveDeactivatedMemberships> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
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
            nameof(RemoveDeactivatedMemberships),
            Guid.NewGuid()
        );
        UserStatusChecker userStatusChecker = new UserStatusChecker(_logger, _configuration);
        var membershipCleaner = scope.ServiceProvider.GetRequiredService<IDeactivatedMemberCleanerApplicationService>();

        try
        {
            await membershipCleaner.RemoveDeactivatedMemberships(userStatusChecker);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while removing deactivated memberships");
        }
    }
}
