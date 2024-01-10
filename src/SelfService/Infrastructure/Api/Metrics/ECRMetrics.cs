using Prometheus;

namespace SelfService.Infrastructure.Api.Metrics;

public static class EcrMetrics
// global class specifically for the prometheus metric, NOT our metrics endpoint
{
    public static Gauge OutOfSyncEcrMetric = global::Prometheus.Metrics.CreateGauge(
        "out_of_sync_ecr_repos_count",
        "number of repos that do not match between our db and ECR"
    );
}
