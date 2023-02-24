using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public static class A
{
    public static CapabilityBuilder Capability => new();
    public static MembershipBuilder Membership => new();
    public static MemberBuilder Member => new();
    public static AwsAccountBuilder AwsAccount => new();
    public static KafkaClusterBuilder KafkaCluster => new();
    public static KafkaTopicBuilder KafkaTopic => new();
    
    public static CapabilityRepositoryBuilder CapabilityRepository => new();
}

public class KafkaTopicBuilder
{
    private KafkaTopicId _id;
    private KafkaClusterId _kafkaClusterId;
    private CapabilityId _capabilityId;
    private KafkaTopicName _name;
    private string _description;
    private KafkaTopicStatusType _status;
    private uint _partitions;
    private long _retention;
    private DateTime _createdAt;
    private string _createdBy;
    private DateTime? _modifiedAt;
    private string? _modifiedBy;

    public KafkaTopicBuilder()
    {
        _id = KafkaTopicId.New();
        _kafkaClusterId = KafkaClusterId.New();
        _capabilityId = CapabilityId.Parse("foo");
        _name = KafkaTopicName.Parse("bar");
        _description = "baz";
        _status = KafkaTopicStatusType.Provisioned;
        _partitions = 1;
        _retention = 1;
        _createdAt = new DateTime(2000, 1, 1);
        _createdBy = nameof(KafkaTopicBuilder);
        _modifiedAt = null;
        _modifiedBy = null;
    }

    public KafkaTopic Build()
    {
        return new KafkaTopic(
            id: _id,
            kafkaClusterId: _kafkaClusterId,
            capabilityId: _capabilityId,
            name: _name,
            description: _description,
            status: _status,
            partitions: _partitions,
            retention: _retention,
            createdAt: _createdAt,
            createdBy: _createdBy,
            modifiedAt: _modifiedAt,
            modifiedBy: _modifiedBy
        );
    }

    public static implicit operator KafkaTopic(KafkaTopicBuilder builder)
        => builder.Build();
}


public class KafkaClusterBuilder
{
    private KafkaClusterId _id;
    private string _realClusterId;
    private string _name;
    private string _description;
    private bool _enabled;

    public KafkaClusterBuilder()
    {
        _id = KafkaClusterId.New();
        _realClusterId = "foo";
        _name = "bar";
        _description = "baz";
        _enabled = true;
    }

    public KafkaCluster Build()
    {
        return new KafkaCluster(
            id: _id,
            realClusterId: _realClusterId,
            name: _name,
            description: _description,
            enabled: _enabled
        );
    }

    public static implicit operator KafkaCluster(KafkaClusterBuilder builder)
        => builder.Build();
}
