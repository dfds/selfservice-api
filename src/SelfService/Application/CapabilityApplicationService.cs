using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Application;

public class CapabilityApplicationService : ICapabilityApplicationService
{
    private readonly ILogger<CapabilityApplicationService> _logger;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IKafkaClusterAccessRepository _kafkaClusterAccessRepository;
    private readonly ITicketingSystem _ticketingSystem;
    private readonly SystemTime _systemTime;

    private const int PendingDaysUntilDeletion = 7;

    public CapabilityApplicationService(
        ILogger<CapabilityApplicationService> logger,
        ICapabilityRepository capabilityRepository,
        IKafkaTopicRepository kafkaTopicRepository,
        IKafkaClusterAccessRepository kafkaClusterAccessRepository,
        ITicketingSystem ticketingSystem,
        SystemTime systemTime
    )
    {
        _logger = logger;
        _capabilityRepository = capabilityRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _kafkaClusterAccessRepository = kafkaClusterAccessRepository;
        _ticketingSystem = ticketingSystem;
        _systemTime = systemTime;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<CapabilityId> CreateNewCapability(
        CapabilityId capabilityId,
        string name,
        string description,
        string requestedBy
    )
    {
        if (await _capabilityRepository.Exists(capabilityId))
        {
            _logger.LogError("Capability with id {CapabilityId} already exists", capabilityId);
            throw EntityAlreadyExistsException<Capability>.WithProperty(x => x.Name, name);
        }
        var creationTime = _systemTime.Now;
        var capability = Capability.CreateCapability(capabilityId, name, description, creationTime, requestedBy);
        await _capabilityRepository.Add(capability);

        return capabilityId;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<KafkaTopicId> RequestNewTopic(
        CapabilityId capabilityId,
        KafkaClusterId kafkaClusterId,
        KafkaTopicName name,
        string description,
        KafkaTopicPartitions partitions,
        KafkaTopicRetention retention,
        string requestedBy
    )
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType}",
            nameof(RequestNewTopic),
            this.GetType().FullName
        );

        if (capabilityId != name.ExtractCapabilityId())
        {
            _logger.LogError(
                "Capability id {CapabilityId} not part of topic name {KafkaTopicName}",
                capabilityId,
                name
            );
            throw new Exception("Capability id not part of topic name.");
        }

        if (!await _capabilityRepository.Exists(capabilityId))
        {
            _logger.LogError("Capability with id {CapabilityId} does not exist", capabilityId);
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        if (await _kafkaTopicRepository.Exists(name, kafkaClusterId))
        {
            _logger.LogError(
                "Topic with name {KafkaTopicName} already exist in cluster {KafkaClusterId}",
                name,
                kafkaClusterId
            );
            throw EntityAlreadyExistsException<KafkaTopic>.WithProperty(x => x.Name, name);
        }

        var topic = KafkaTopic.RequestNew(
            kafkaClusterId: kafkaClusterId,
            capabilityId: capabilityId,
            name: name,
            description: description,
            partitions: partitions,
            retention: retention,
            createdAt: _systemTime.Now,
            createdBy: requestedBy
        );

        await _kafkaTopicRepository.Add(topic);

        _logger.LogInformation(
            "New topic {KafkaTopicName} for capability {CapabilityId} has been requested by {RequestedBy}",
            topic.Name,
            capabilityId,
            requestedBy
        );

        return topic.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task RequestKafkaClusterAccess(
        CapabilityId capabilityId,
        KafkaClusterId kafkaClusterId,
        UserId requestedBy
    )
    {
        var kafkaClusterAccess = await _kafkaClusterAccessRepository.FindBy(capabilityId, kafkaClusterId);
        if (kafkaClusterAccess is not null)
        {
            // there's already a cluster access request - ignore for now
            return;
        }

        kafkaClusterAccess = KafkaClusterAccess.RequestAccess(
            capabilityId,
            kafkaClusterId,
            _systemTime.Now,
            requestedBy
        );

        await _kafkaClusterAccessRepository.Add(kafkaClusterAccess);
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterKafkaClusterAccessGranted(CapabilityId capabilityId, KafkaClusterId kafkaClusterId)
    {
        var kafkaClusterAccess = await _kafkaClusterAccessRepository.FindBy(capabilityId, kafkaClusterId);
        if (kafkaClusterAccess is null)
        {
            throw new InvalidOperationException(
                $"Unable to find Kafka access request for {capabilityId} on cluster {kafkaClusterId}"
            );
        }

        kafkaClusterAccess.RegisterAsGranted(_systemTime.Now);
    }

    [TransactionalBoundary, Outboxed]
    public async Task RequestCapabilityDeletion(CapabilityId capabilityId, UserId userId)
    {
        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability is null)
        {
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }
        var modificationTime = _systemTime.Now;
        capability.RequestDeletion(userId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task CancelCapabilityDeletionRequest(CapabilityId capabilityId, UserId userId)
    {
        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability is null)
        {
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }
        var modificationTime = _systemTime.Now;
        capability.CancelDeletionRequest(userId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task CheckPendingCapabilityDeletions()
    {
        var capabilities = await _capabilityRepository.GetAllPendingDeletion();
        foreach (var capability in capabilities)
        {
            if (capability.HasBeenPendingFor(days: PendingDaysUntilDeletion))
            {
                var message =
                    "*Capability Deletion Request*\n"
                    + "\nThe following capability has been pending deletion for 7 days and will be deleted in 24 hours:\n"
                    + $"Capability Name=\"{capability.Name}\" \\\n"
                    + $"Capability Id=\"{capability.Id}\" \\\n"
                    + $"Deletion Requested by user=\"{capability.ModifiedBy}\" \\\n"
                    + $"Originally Created by user=\"{capability.CreatedBy}\"";
                var headers = new Dictionary<string, string>();
                headers["CAPABILITY_NAME"] = capability.Name;
                headers["CAPABILITY_ID"] = capability.Id;
                headers["CAPABILITY_CREATED_BY"] = capability.CreatedBy;
                headers["DELETION_REQUESTED_AT"] = capability.ModifiedAt.ToString();
                headers["DELETION_REQUESTED_BY"] = capability.ModifiedBy;

                await _ticketingSystem.CreateTicket(message, headers);

                capability.MarkAsDeleted();
            }
        }
    }
}
