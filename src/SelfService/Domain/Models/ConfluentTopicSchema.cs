using System;

namespace SelfService.Domain.Models;

public class ConfluentTopicMetadata
{
    public ConfluentTopicMetadata(
        string self,
        string resourceName
    )
    {
        Self = self;
        ResourceName = resourceName;
    }

    public string Self { get; set; }
    public string ResourceName { get; set; }
}

public class ConfluentRelation
{
    public ConfluentRelation(
        string related
    )
    {
        Related = related;
    }

    public string Related { get; set; }
}

public class ConfluentTopic
{
    public ConfluentTopic(
        string kind,
        string clusterId,
        string topicName,
        bool isInternal,
        int replicationFactor,
        int partitionsCount,
        ConfluentTopicMetadata metadata,
        List<ConfluentRelation> relations,
        List<ConfluentRelation> configs,
        List<ConfluentRelation> partitionReassignments
    )
    {
        Kind = kind;
        ClusterId = clusterId;
        TopicName = topicName;
        IsInternal = isInternal;
        ReplicationFactor = replicationFactor;
        PartitionsCount = partitionsCount;
        Metadata = metadata;
        Relations = relations;
        Configs = configs;
        PartitionReassignments = partitionReassignments;
    }
    public string Kind { get; set; }
    public string ClusterId { get; set; }
    public string TopicName { get; set; }
    public bool IsInternal { get; set; }
    public int ReplicationFactor { get; set; }
    public int PartitionsCount { get; set; }
    public ConfluentTopicMetadata Metadata { get; set; }
    public List<ConfluentRelation> Relations { get; set; }
    public List<ConfluentRelation> Configs { get; set; }
    public List<ConfluentRelation> PartitionReassignments { get; set; }
}
