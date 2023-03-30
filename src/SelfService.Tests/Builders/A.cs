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
    public static MembershipApplicationBuilder MembershipApplication => new();
    public static MembershipApprovalBuilder MembershipApproval => new();
    public static MessageContractBuilder MessageContract => new();
    
    public static CapabilityRepositoryBuilder CapabilityRepository => new();
}

public class MessageContractBuilder
{
    private MessageContractId _id;
    private KafkaTopicId _kafkaTopicId;
    private MessageType _messageType;
    private string _description;
    private MessageContractExample _example;
    private MessageContractSchema _schema;
    private MessageContractStatus _status;
    private DateTime _createdAt;
    private string _createdBy;
    private DateTime? _modifiedAt;
    private string? _modifiedBy;

    public MessageContractBuilder()
    {
        _id = MessageContractId.New();
        _kafkaTopicId = KafkaTopicId.New();
        _messageType = MessageType.Parse("foo");
        _description = "bar";
        _example = MessageContractExample.Parse("baz");
        _schema = MessageContractSchema.Parse("qux");
        _status = MessageContractStatus.Provisioned;
        _createdAt = new DateTime(2000, 1, 1);
        _createdBy = nameof(MessageContractBuilder);
        _modifiedAt = null;
        _modifiedBy = null;
    }

    public MessageContract Build()
    {
        return new MessageContract(
            id: _id,
            kafkaTopicId: _kafkaTopicId,
            messageType: _messageType,
            description: _description,
            example: _example,
            schema: _schema,
            status: _status,
            createdAt: _createdAt,
            createdBy: _createdBy,
            modifiedAt: _modifiedAt,
            modifiedBy: _modifiedBy
        );
    }

    public static implicit operator MessageContract(MessageContractBuilder builder)
        => builder.Build();
}

public class MembershipApplicationBuilder
{
    private MembershipApplicationId _id;
    private CapabilityId _capability;
    private UserId _applicant;
    private MembershipApplicationStatusOptions _status;
    private DateTime _submittedAt;
    private DateTime _expiresOn;
    private IEnumerable<MembershipApproval> _approvals;

    public MembershipApplicationBuilder()
    {
        _id = MembershipApplicationId.New();
        _capability = CapabilityId.Parse("foo");
        _applicant = UserId.Parse("bar");
        _status = MembershipApplicationStatusOptions.PendingApprovals;
        _submittedAt = new DateTime(2000, 1, 1);
        _expiresOn = _submittedAt.AddDays(1);
        _approvals = Enumerable.Empty<MembershipApproval>();
    }
    
    public MembershipApplicationBuilder WithApproval(Action<MembershipApprovalBuilder> modifier)
    {
        var builder = new MembershipApprovalBuilder();
        modifier(builder);
        return WithApprovals(builder.Build());
    }
    
    public MembershipApplicationBuilder WithApprovals(params MembershipApproval[] approvals)
    {
        _approvals = approvals;
        return this;
    }
    
    public MembershipApplicationBuilder WithApprovals(IEnumerable<MembershipApproval> approvals)
    {
        _approvals = approvals;
        return this;
    }
    
    public MembershipApplication Build()
    {
        return new MembershipApplication(
            id: _id,
            capabilityId: _capability,
            applicant: _applicant,
            approvals: _approvals,
            status: _status,
            submittedAt: _submittedAt,
            expiresOn: _expiresOn
        );
    }
}

public class MembershipApprovalBuilder
{
    private Guid _id;
    private UserId _approvedBy;
    private DateTime _approvedAt;

    public MembershipApprovalBuilder()
    {
        _id = Guid.NewGuid();
        _approvedBy = UserId.Parse("foo");
        _approvedAt = new DateTime(2000, 1, 1);
    }
    
    public MembershipApprovalBuilder WithApprovedBy(UserId approvedBy)
    {
        _approvedBy = approvedBy;
        return this;
    }
    
    public MembershipApproval Build()
    {
        return new MembershipApproval(
            id: _id,
            approvedBy: _approvedBy,
            approvedAt: _approvedAt
        );
    }
}

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
    private string _name;
    private string _description;
    private bool _enabled;

    public KafkaClusterBuilder()
    {
        _id = KafkaClusterId.Parse("cluster foo");
        _name = "bar";
        _description = "baz";
        _enabled = true;
    }

    public KafkaCluster Build()
    {
        return new KafkaCluster(
            id: _id,
            name: _name,
            description: _description,
            enabled: _enabled
        );
    }

    public static implicit operator KafkaCluster(KafkaClusterBuilder builder)
        => builder.Build();
}
