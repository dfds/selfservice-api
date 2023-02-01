namespace SelfService.Legacy;

public class Synchronizer : BackgroundService
{
    private readonly ILogger<Synchronizer> _logger;

    public Synchronizer(ILogger<Synchronizer> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Synchronizing legacy data");

            await Task.Delay(10_000, stoppingToken);
        }
    }
}