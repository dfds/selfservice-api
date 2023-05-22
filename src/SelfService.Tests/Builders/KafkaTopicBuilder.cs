using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class KafkaTopicBuilder
{
    private KafkaTopicId _id;
    private KafkaClusterId _kafkaClusterId;
    private CapabilityId _capabilityId;
    private KafkaTopicName _name;
    private string _description;
    private KafkaTopicStatusType _status;
    private KafkaTopicPartitions _partitions;
    private KafkaTopicRetention _retention;
    private DateTime _createdAt;
    private string _createdBy;
    private DateTime? _modifiedAt;
    private string? _modifiedBy;

    public KafkaTopicBuilder()
    {
        _id = KafkaTopicId.New();
        _kafkaClusterId = KafkaClusterId.Parse("cluster foo");
        _capabilityId = CapabilityId.Parse("foo");
        _name = KafkaTopicName.Parse("bar");
        _description = "baz";
        _status = KafkaTopicStatusType.Provisioned;
        _partitions = KafkaTopicPartitions.One;
        _retention = KafkaTopicRetention.OneDay;
        _createdAt = new DateTime(2000, 1, 1);
        _createdBy = nameof(KafkaTopicBuilder);
        _modifiedAt = null;
        _modifiedBy = null;
    }

    public KafkaTopicBuilder WithId(KafkaTopicId id)
    {
        _id = id;
        return this;
    }

    public KafkaTopicBuilder WithName(string name)
    {
        _name = name;
        return this;
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