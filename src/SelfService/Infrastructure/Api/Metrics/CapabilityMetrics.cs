using Prometheus;

namespace SelfService.Infrastructure.Api.Metrics;

public static class CapabilityMetrics
{
    public static Gauge CapabilityMetric = global::Prometheus.Metrics.CreateGauge(
        "selfservice_capability",
        "Capability data",
        new GaugeConfiguration
        {
            LabelNames = new []{"name", "id", "aws_acccount_id", "cost_centre"}
        }
    );
}
