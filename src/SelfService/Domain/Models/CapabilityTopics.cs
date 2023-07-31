namespace SelfService.Domain.Models;

public class CapabilityTopics
{
    public CapabilityTopics(Capability capability, ClusterTopics[] clusters)
    {
        Capability = capability;
        Clusters = clusters;
    }

    public Capability Capability { get; }
    public ClusterTopics[] Clusters { get; }
}

public class ClusterTopics
{
    public ClusterTopics(KafkaCluster cluster, KafkaTopic[] topics)
    {
        Cluster = cluster;
        Topics = topics;
    }

    public KafkaCluster Cluster { get; }
    public KafkaTopic[] Topics { get; }
}
