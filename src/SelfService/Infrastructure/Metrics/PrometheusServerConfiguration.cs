using Prometheus;

namespace SelfService.Infrastructure.Metrics;

public static class PrometheusServerConfiguration
{
    public static void AddMetrics(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<MetricHostedService>();
    }

    private class MetricHostedService : IHostedService, IDisposable
    {
        private const string Host = "0.0.0.0";
        private const int Port = 8888;

        private readonly IMetricServer _metricServer = new KestrelMetricServer(Host, Port);


        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Starting metric server on {Host}:{Port}");

            _metricServer.Start();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Shutting down metric server");
            await _metricServer.StopAsync();
            Console.WriteLine("Done shutting down metric server");
        }

        public void Dispose()
        {
            _metricServer.Dispose();
        }
    }
}