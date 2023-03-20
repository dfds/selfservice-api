using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class MessageContract : AggregateRoot<MessageContractId>
{
    public MessageContract(MessageContractId id, KafkaTopicId kafkaTopicId, MessageType messageType, string description,
        MessageContractExample example, MessageContractSchema schema, MessageContractStatus status, DateTime createdAt,
        string createdBy, DateTime? modifiedAt, string? modifiedBy) : base(id)
    {
        MessageType = messageType;
        Example = example;
        Schema = schema;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
        KafkaTopicId = kafkaTopicId;
        Description = description;
        Status = status;
    }

    public KafkaTopicId KafkaTopicId { get; private set; }
    public MessageType MessageType { get; private set; }
    public MessageContractExample Example { get; private set; }
    public MessageContractSchema Schema { get; private set; }

    public string Description { get; private set; }

    public void ChangeDescription(string newDescription, DateTime modifiedAt, string modifiedBy)
    {
        Description = newDescription;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }

    public MessageContractStatus Status { get; private set; }

    private void ChangeStatus(MessageContractStatus newStatus, DateTime modifiedAt, string modifiedBy)
    {
        if (Status == MessageContractStatus.Provisioned)
        {
            if (newStatus == MessageContractStatus.Provisioned)
            {
                return; // already provisioned - just ignore
            }

            throw new Exception($"Message contract \"{MessageType} (#{Id})\" has been provisioned and cannot change status!");
        }

        Status = newStatus;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;

        if (Status == MessageContractStatus.Provisioned)
        {
            Raise(new NewMessageContractHasBeenProvisioned
            {
                MessageContractId = Id.ToString(),
                KafkaTopicId = KafkaTopicId.ToString(),
                MessageType = MessageType.ToString(),
            });
        }
    }

    public void RegisterAsInProgress(DateTime modifiedAt, string modifiedBy)
        => ChangeStatus(MessageContractStatus.InProgress, modifiedAt, modifiedBy);

    public void RegisterAsProvisioned(DateTime modifiedAt, string modifiedBy)
        => ChangeStatus(MessageContractStatus.Provisioned, modifiedAt, modifiedBy);

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    public static MessageContract RequestNew(KafkaTopicId kafkaTopicId, MessageType messageType, string description,
        MessageContractExample example, MessageContractSchema schema, DateTime createdAt, string createdBy)
    {
        var instance = new MessageContract(
            id: MessageContractId.New(),
            kafkaTopicId: kafkaTopicId,
            messageType: messageType,
            description: description,
            example: example,
            schema: schema, 
            status: MessageContractStatus.Requested, 
            createdAt: createdAt,
            createdBy: createdBy,
            modifiedAt: null,
            modifiedBy: null
        );

        instance.Raise(new NewMessageContractHasBeenRequested
        {
            MessageContractId = instance.Id.ToString(),
            KafkaTopicId = instance.KafkaTopicId.ToString(),
            MessageType = instance.MessageType.ToString(),
            Schema = instance.Schema.ToString(),
            Description = instance.Description,
        });

        return instance;
    }
}