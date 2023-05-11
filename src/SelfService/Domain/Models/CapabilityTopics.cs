namespace SelfService.Domain.Models;

public class CapabilityTopics
{
    public CapabilityTopics(KafkaCluster cluster, KafkaTopic[] topics)
    {
        Cluster = cluster;
        Topics = topics;
    }

    public KafkaCluster Cluster { get; }
    public KafkaTopic[] Topics { get; }
}