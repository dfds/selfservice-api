using System.Diagnostics.Metrics;

namespace SelfService.Infrastructure.Api.Metrics;

public static class EcrMetrics
// global class specifically for the prometheus metric, NOT our metrics endpoint
{
    public static Gauge<long> OutOfSyncEcrMetric = CapabilityMetrics.SelfServiceMeter.CreateGauge<long>(
        "out_of_sync_ecr_repos_count",
        description: "number of repos that do not match between our db and ECR"
    );
}
