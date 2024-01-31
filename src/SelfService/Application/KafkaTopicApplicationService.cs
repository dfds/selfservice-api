using System.Text.Json;
using Json.More;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class KafkaTopicApplicationService : IKafkaTopicApplicationService
{
    private readonly ILogger<KafkaTopicApplicationService> _logger;
    private readonly IMessageContractRepository _messageContractRepository;
    private readonly SystemTime _systemTime;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;

    public KafkaTopicApplicationService(
        ILogger<KafkaTopicApplicationService> logger,
        IMessageContractRepository messageContractRepository,
        SystemTime systemTime,
        IKafkaTopicRepository kafkaTopicRepository
    )
    {
        _logger = logger;
        _messageContractRepository = messageContractRepository;
        _systemTime = systemTime;
        _kafkaTopicRepository = kafkaTopicRepository;
    }

    public async Task ValidateRequestForCreatingNewContract(
        KafkaTopicId kafkaTopicId,
        MessageType messageType,
        MessageContractSchema newSchema
    )
    {
        var requestedSchemaVersion = newSchema.GetSchemaVersion();
        if (requestedSchemaVersion == null)
            throw new InvalidMessageContractRequestException(
                "Cannot request new message contract without schema version"
            );

        var latestContract = (await _messageContractRepository.GetLatestSchema(kafkaTopicId, messageType));
        if (latestContract == null)
        {
            if (requestedSchemaVersion != 1)
                throw new InvalidMessageContractRequestException(
                    "Cannot request new message contract with schema version other than 1"
                );
            return;
        }

        if (requestedSchemaVersion != latestContract.SchemaVersion + 1)
        {
            throw new InvalidMessageContractRequestException(
                $"Cannot request new message contract with schema version {requestedSchemaVersion} as the latest version is {latestContract.SchemaVersion}"
            );
        }

        if (latestContract.Status != KafkaTopicStatus.Provisioned)
        {
            throw new InvalidMessageContractRequestException(
                $"Cannot request new message contract as the previous message contract has state: {latestContract.Status}"
            );
        }

        JsonDocument previousSchemaDocument = JsonDocument.Parse(latestContract.Schema.ToString());
        JsonDocument newSchemaDocument = JsonDocument.Parse(newSchema);
        CheckIsBackwardCompatible(previousSchemaDocument, newSchemaDocument);
    }

    private void CheckIsBackwardCompatible(JsonDocument previousSchemaDocument, JsonDocument newSchemaDocument)
    {
        bool GetAdditionalProperties(JsonDocument doc)
        {
            var additionalProperties = true;
            try
            {
                additionalProperties = doc.RootElement.GetProperty("additionalProperties").GetBoolean();
            }
            catch { }

            return additionalProperties;
        }

        List<string> GetRequired(JsonDocument doc)
        {
            var required = new List<string>();
            try
            {
                foreach (var property in doc.RootElement.GetProperty("required").EnumerateArray())
                {
                    required.Add(property.GetString()!);
                }
            }
            catch { }

            return required;
        }

        // See: https://yokota.blog/2021/03/29/understanding-json-schema-compatibility/


        var previousSchemaRequired = GetRequired(previousSchemaDocument);
        var newSchemaRequired = GetRequired(newSchemaDocument);

        // default value for confluent cloud schemas
        bool previousSchemaIsOpenContentModel = GetAdditionalProperties(previousSchemaDocument);
        bool newSchemaIsOpenContentModel = GetAdditionalProperties(newSchemaDocument);

        if (previousSchemaIsOpenContentModel && !newSchemaIsOpenContentModel)
            throw new InvalidMessageContractRequestException(
                $"Cannot change schema from open content model to closed content model"
            );

        // Simplified port of https://github.dev/confluentinc/schema-registry
        HashSet<string> propertyKeys = new HashSet<string>();
        try
        {
            foreach (var property in previousSchemaDocument.RootElement.GetProperty("properties").EnumerateObject())
            {
                propertyKeys.Add(property.Name);
            }
        }
        catch
        {
            // suppress exception: ok if previous schema doesnt have properties
        }

        try
        {
            foreach (var property in newSchemaDocument.RootElement.GetProperty("properties").EnumerateObject())
            {
                propertyKeys.Add(property.Name);
            }
        }
        catch
        {
            // suppress exception: ok if new schema doesnt have properties
        }

        JsonElement? GetPropertyOrNull(JsonDocument doc, string key)
        {
            try
            {
                return doc.RootElement.GetProperty("properties").GetProperty(key);
            }
            catch
            {
                return null;
            }
        }

        foreach (var propertyKey in propertyKeys)
        {
            var previousProperty = GetPropertyOrNull(previousSchemaDocument, propertyKey);
            var newProperty = GetPropertyOrNull(newSchemaDocument, propertyKey);

            if (newProperty == null)
            {
                if (newSchemaIsOpenContentModel)
                    continue;

                throw new InvalidMessageContractRequestException(
                    $"Not allowed to remove properties from closed content model"
                );
            }

            if (previousProperty == null)
            {
                if (previousSchemaIsOpenContentModel)
                {
                    throw new InvalidMessageContractRequestException(
                        $"Not allowed to add properties to open content model"
                    );
                }

                if (newSchemaRequired.Contains(propertyKey))
                {
                    // Not allowed to add required properties to closed content model
                    throw new InvalidMessageContractRequestException($"Not allowed to add new required properties");
                }

                continue;
            }

            CheckIsBackwardCompatibleSchema(previousProperty.Value, newProperty.Value);
        }
    }

    private void CheckIsBackwardCompatibleSchema(JsonElement prevSchema, JsonElement newSchema)
    {
        if (prevSchema.ValueKind != newSchema.ValueKind)
        {
            throw new InvalidMessageContractRequestException(
                $"Cannot change schema type from {prevSchema.ValueKind} to {newSchema.ValueKind} for property {prevSchema}"
            );
        }

        switch (newSchema.ValueKind)
        {
            case JsonValueKind.Object:
            {
                CheckIsBackwardCompatible(prevSchema.ToJsonDocument(), newSchema.ToJsonDocument());
                break;
            }
            case JsonValueKind.Array:
            {
                for (int i = 0; i < newSchema.GetArrayLength(); i++)
                {
                    CheckIsBackwardCompatibleSchema(prevSchema[i], newSchema[i]);
                }

                break;
            }
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task<MessageContractId> RequestNewMessageContract(
        KafkaTopicId kafkaTopicId,
        MessageType messageType,
        string description,
        MessageContractExample example,
        MessageContractSchema schema,
        string requestedBy,
        bool enforceSchemaEnvelope
    )
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} requested by {RequestedBy}",
            nameof(RequestNewMessageContract),
            GetType().FullName,
            requestedBy
        );

        await ValidateRequestForCreatingNewContract(kafkaTopicId, messageType, schema);

        if (enforceSchemaEnvelope)
        {
            schema.ValidateSchemaEnvelope();
        }

        var topic = await _kafkaTopicRepository.Get(kafkaTopicId);

        int schemaVersion = (int)schema.GetSchemaVersion()!;
        var messageContract = MessageContract.RequestNew(
            kafkaTopicId: kafkaTopicId,
            messageType: messageType,
            kafkaTopicName: topic.Name,
            capabilityId: topic.CapabilityId,
            kafkaClusterId: topic.KafkaClusterId,
            description: description,
            example: example,
            schema: schema,
            createdAt: _systemTime.Now,
            createdBy: requestedBy,
            schemaVersion: schemaVersion
        );

        await _messageContractRepository.Add(messageContract);

        _logger.LogInformation(
            "New message contract {MessageContractId} for message type {MessageType} has been added to topic {KafkaTopicName}",
            messageContract.Id,
            messageContract.MessageType,
            topic.Name
        );

        return messageContract.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task RetryRequestNewMessageContract(
        KafkaTopicId kafkaTopicId,
        MessageContractId messageContractId,
        string requestedBy
    )
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} requested by {RequestedBy}",
            nameof(RequestNewMessageContract),
            GetType().FullName,
            requestedBy
        );

        var storedContract = await _messageContractRepository.Get(messageContractId);

        if (storedContract == null || storedContract.KafkaTopicId != kafkaTopicId)
        {
            throw new EntityNotFoundException<MessageContract>(
                $"Message contract \"{messageContractId}\" does not exist on topic \"{kafkaTopicId}\"."
            );
        }

        if (storedContract.Status != MessageContractStatus.Failed)
        {
            throw new ArgumentException(
                "Unable to retry message contract creation: Message contract is not in failed state."
            );
        }

        var storedTopic = await _kafkaTopicRepository.Get(kafkaTopicId);

        MessageContract.Retry(storedContract, storedTopic);

        storedContract.RegisterAsRequested(_systemTime.Now, requestedBy);
        _messageContractRepository.Update(storedContract);

        _logger.LogInformation(
            "Retrying creation of message contract {MessageContractId} for message type {MessageType} and topic {KafkaTopicName}",
            storedContract.Id,
            storedContract.MessageType,
            storedTopic.Name
        );
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterMessageContractAsProvisioned(MessageContractId messageContractId, string changedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(RegisterMessageContractAsProvisioned),
            GetType().FullName,
            changedBy
        );

        var messageContract = await _messageContractRepository.Get(messageContractId);
        messageContract.RegisterAsProvisioned(_systemTime.Now, changedBy);
        messageContract.RaiseNewMessageContractHasBeenProvisioned();

        _logger.LogInformation(
            "Message contract {MessageContractId} for message type {MessageType} has been provisioned.",
            messageContract.Id,
            messageContract.MessageType
        );
    }

    [TransactionalBoundary]
    public async Task RegisterMessageContractAsFailed(MessageContractId messageContractId, string changedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(RegisterMessageContractAsFailed),
            GetType().FullName,
            changedBy
        );

        var messageContract = await _messageContractRepository.Get(messageContractId);
        messageContract.RegisterAsFailed(_systemTime.Now, changedBy);

        _logger.LogInformation(
            "Message contract {MessageContractId} for message type {MessageType} has been marked as failed",
            messageContract.Id,
            messageContract.MessageType
        );
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterKafkaTopicAsInProgress(KafkaTopicId kafkaTopicId, string changedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(RegisterMessageContractAsProvisioned),
            GetType().FullName,
            changedBy
        );

        var kafkaTopic = await _kafkaTopicRepository.FindBy(kafkaTopicId);
        if (kafkaTopic is not null)
        {
            kafkaTopic.RegisterAsInProgress(_systemTime.Now, changedBy);
            _logger.LogInformation(
                "Kafka topic provisioning for \"{KafkaTopicName}\" is now in progress",
                kafkaTopic.Name
            );
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterKafkaTopicAsProvisioned(KafkaTopicId kafkaTopicId, string changedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(RegisterMessageContractAsProvisioned),
            GetType().FullName,
            changedBy
        );

        var kafkaTopic = await _kafkaTopicRepository.FindBy(kafkaTopicId);

        if (kafkaTopic is not null)
        {
            kafkaTopic.RegisterAsProvisioned(_systemTime.Now, changedBy);
            _logger.LogInformation("Kafka topic {KafkaTopicName} has now been provisioned", kafkaTopic.Name);
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task ChangeKafkaTopicDescription(KafkaTopicId kafkaTopicId, string newDescription, string changedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(ChangeKafkaTopicDescription),
            GetType().FullName,
            changedBy
        );

        var kafkaTopic = await _kafkaTopicRepository.Get(kafkaTopicId);
        kafkaTopic.ChangeDescription(newDescription, _systemTime.Now, changedBy);

        _logger.LogDebug("Description of kafka topic {KafkaTopicName} has now been changed", kafkaTopic.Name);
    }

    [TransactionalBoundary, Outboxed]
    public async Task DeleteKafkaTopic(KafkaTopicId kafkaTopicId, string requestedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(DeleteKafkaTopic),
            GetType().FullName,
            requestedBy
        );

        var kafkaTopic = await _kafkaTopicRepository.Get(kafkaTopicId);
        kafkaTopic.Delete();

        await _kafkaTopicRepository.Delete(kafkaTopic);

        _logger.LogInformation(
            "Kafka topic (#{KafkaTopicId}) {KafkaTopicName} has now been deleted by {UserId}",
            kafkaTopic.Id,
            kafkaTopic.Name,
            requestedBy
        );
    }

    [TransactionalBoundary, Outboxed]
    public async Task DeleteAssociatedMessageContracts(KafkaTopicId kafkaTopicId, string requestedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(DeleteAssociatedMessageContracts),
            GetType().FullName,
            requestedBy
        );

        var messageContracts = await _messageContractRepository.FindBy(kafkaTopicId);
        foreach (var contract in messageContracts)
        {
            await _messageContractRepository.Delete(contract);

            _logger.LogInformation(
                "Deleted message contract (#{MessageContractId}) {MessageContractType} on topic {KafkaTopicId}",
                contract.Id,
                contract.MessageType,
                kafkaTopicId
            );
        }

        _logger.LogInformation(
            "All message contracts for Kafka topic (#{KafkaTopicId}) has now been deleted by {UserId}",
            kafkaTopicId,
            requestedBy
        );
    }
}
