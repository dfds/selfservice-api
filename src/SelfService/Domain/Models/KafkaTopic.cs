using System.Text.RegularExpressions;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class KafkaTopic : AggregateRoot<KafkaTopicId>
{
    public KafkaTopic(KafkaTopicId id, KafkaClusterId kafkaClusterId, CapabilityId capabilityId, KafkaTopicName name, string description,
        KafkaTopicStatusType status, KafkaTopicPartitions partitions, KafkaTopicRetention retention, DateTime createdAt, string createdBy, DateTime? modifiedAt, string? modifiedBy) : base(id)
    {
        KafkaClusterId = kafkaClusterId;
        CapabilityId = capabilityId;
        Name = name;
        Description = description;
        Status = status;
        Partitions = partitions;
        Retention = retention;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }

    public CapabilityId CapabilityId { get; private set; }
    public KafkaClusterId KafkaClusterId { get; private set; }
    public KafkaTopicName Name { get; private set; }

    public string Description { get; private set; }

    public void ChangeDescription(string newDescription, DateTime modifiedAt, string modifiedBy)
    {
        Description = newDescription;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }
        
    public KafkaTopicStatusType Status { get; private set; }

    private void ChangeStatus(KafkaTopicStatusType newStatus, DateTime modifiedAt, string modifiedBy)
    {
        Status = newStatus;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;

        // raise event??
    }

    public void RegisterAsInProgress(DateTime modifiedAt, string modifiedBy) 
        => ChangeStatus(KafkaTopicStatusType.InProgress, modifiedAt, modifiedBy);

    public void RegisterAsProvisioned(DateTime modifiedAt, string modifiedBy) 
        => ChangeStatus(KafkaTopicStatusType.Provisioned, modifiedAt, modifiedBy);

    public KafkaTopicPartitions Partitions { get; private set; }
    public KafkaTopicRetention Retention { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }
        
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    public static KafkaTopic RequestNew(KafkaClusterId kafkaClusterId, CapabilityId capabilityId, KafkaTopicName name, string description, 
        KafkaTopicPartitions partitions, KafkaTopicRetention retention, DateTime createdAt, string createdBy)
    {
        var instance = new KafkaTopic(
            id: KafkaTopicId.New(),
            kafkaClusterId: kafkaClusterId,
            capabilityId: capabilityId,
            name: name,
            description: description,
            status: KafkaTopicStatusType.Requested,
            partitions: partitions,
            retention: retention,
            createdAt: createdAt,
            createdBy: createdBy,
            modifiedAt: null,
            modifiedBy: null
        );

        instance.Raise(new NewKafkaTopicHasBeenRequested
        {
            KafkaTopicId = instance.Id.ToString(),
            KafkaClusterId = kafkaClusterId.ToString(),
            CapabilityId = capabilityId.ToString(),

        });

        return instance;
    }
}

public class KafkaTopicPartitions : ValueObject
{
    public static KafkaTopicPartitions One = new(1);
    public static KafkaTopicPartitions Three = new(3);
    public static KafkaTopicPartitions Six = new(6);

    private readonly uint _value;

    private KafkaTopicPartitions(uint value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString();
    }

    public static KafkaTopicPartitions From(uint value)
    {
        if (TryCreate(value, out var partitions))
        {
            return partitions;
        }

        throw new ArgumentOutOfRangeException(nameof(value), $"Value \"{value}\" is not supported as partition count.");
    }

    public static bool TryCreate(uint value, out KafkaTopicPartitions partitions)
    {
        var supportedValues = new uint[] { One, Three, Six };

        if (supportedValues.Contains(value))
        {
            partitions = new KafkaTopicPartitions(value);
            return true;
        }

        partitions = null!;
        return false;
    }

    public static implicit operator KafkaTopicPartitions(uint value) => new(value);
    public static implicit operator uint(KafkaTopicPartitions partitions) => partitions._value;
}

public class KafkaTopicRetention : ValueObject
{
    public static KafkaTopicRetention OneDay = new KafkaTopicRetention("1d");
    public static KafkaTopicRetention SevenDays = new KafkaTopicRetention("7d");
    public static KafkaTopicRetention ThirtyOneDays = new KafkaTopicRetention("31d");
    public static KafkaTopicRetention OneYear = new KafkaTopicRetention("365d");
    public static KafkaTopicRetention Forever = new KafkaTopicRetention("forever");

    private readonly string _value;

    private KafkaTopicRetention(string value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static KafkaTopicRetention Parse(string? text)
    {
        if (TryParse(text, out var retention))
        {
            return retention;
        }

        throw new FormatException($"Value \"{text}\" is not a valid/supported topic retention.");
    }

    public static bool TryParse(string? text, out KafkaTopicRetention retention)
    {
        var candidate = text ?? "";

        var match = Regex.Match(candidate, @"^(?<days>\d+)[dD]$");
        if ((match.Success && match.Groups["days"].Value != "0") || "forever".Equals(candidate, StringComparison.InvariantCultureIgnoreCase))
        {
            retention = new KafkaTopicRetention(text!.ToLowerInvariant());
            return true;
        }

        retention = null!;
        return false;
    }

    public static implicit operator string(KafkaTopicRetention retention) => retention._value;
    public static implicit operator KafkaTopicRetention(string value) => Parse(value);
}