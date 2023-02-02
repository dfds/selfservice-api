namespace SelfService.Legacy;

public class Synchronizer : BackgroundService
{
    private readonly ILogger<Synchronizer> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly int _syncInterval;

    public Synchronizer(ILogger<Synchronizer> logger, IServiceScopeFactory serviceScopeFactory, int syncInterval)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _syncInterval = syncInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Synchronizing legacy data");

            await SynchronizeCapabilities(stoppingToken);
            await SynchronizeKafka(stoppingToken);

            _logger.LogDebug("Synchronizing again in {Timeout} seconds", _syncInterval);

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }

    private async Task SynchronizeCapabilities(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var synchronizer = scope.ServiceProvider.GetRequiredService<CapabilitySynchronizer>();
        await synchronizer.Synchronize(stoppingToken);
    }

    private async Task SynchronizeKafka(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var synchronizer = scope.ServiceProvider.GetRequiredService<KafkaSynchronizer>();
        await synchronizer.Synchronize(stoppingToken);
    }
}