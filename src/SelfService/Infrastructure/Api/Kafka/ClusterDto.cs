using SelfService.Legacy.Models;

namespace SelfService.Infrastructure.Api.Kafka;

internal record ClusterDto(string? Name, string? Description, bool Enabled, string? ClusterId)
{
    public static ClusterDto Create(Cluster cluster)
    {
        return new ClusterDto(cluster.Name, cluster.Description, cluster.Enabled, cluster.ClusterId);
    }
}