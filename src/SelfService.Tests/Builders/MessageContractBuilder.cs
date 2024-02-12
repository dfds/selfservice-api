using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

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
    private int _schemaVersion;

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
        _schemaVersion = 1;
    }

    public MessageContractBuilder WithId(MessageContractId id)
    {
        _id = id;
        return this;
    }

    public MessageContractBuilder WithKafkaTopicId(KafkaTopicId kafkaTopicId)
    {
        _kafkaTopicId = kafkaTopicId;
        return this;
    }

    public MessageContractBuilder WithSchema(MessageContractSchema schema)
    {
        _schema = schema;
        return this;
    }

    public MessageContractBuilder WithSchemaVersion(int schemaVersion)
    {
        _schemaVersion = schemaVersion;
        return this;
    }

    public MessageContractBuilder WithType(MessageType type)
    {
        _messageType = type;
        return this;
    }

    public MessageContractBuilder WithStatus(MessageContractStatus status)
    {
        _status = status;
        return this;
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
            modifiedBy: _modifiedBy,
            schemaVersion: _schemaVersion
        );
    }

    public static implicit operator MessageContract(MessageContractBuilder builder) => builder.Build();
}
