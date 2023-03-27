using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Legacy.Models;
using Capability = SelfService.Domain.Models.Capability;

namespace SelfService.Application;

public class CapabilityApplicationService : ICapabilityApplicationService
{
    private readonly ILogger<CapabilityApplicationService> _logger;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly SystemTime _systemTime;

    public CapabilityApplicationService(ILogger<CapabilityApplicationService> logger, ICapabilityRepository capabilityRepository,
        IKafkaTopicRepository kafkaTopicRepository, SystemTime systemTime)
    {
        _logger = logger;
        _capabilityRepository = capabilityRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _systemTime = systemTime;
    }
    [TransactionalBoundary]
    public async Task<CapabilityId> CreateNewCapability(CapabilityId capabilityId, string name, string description,
        string requestedBy)
    {
        if (await _capabilityRepository.Exists(capabilityId))
        {
            _logger.LogError("Capability with id {CapabilityId} already exists", capabilityId);
            throw EntityAlreadyExistsException<Capability>.WithProperty(x => x.Name, name);
        }
        var creationTime = _systemTime.Now;
        var capability = new Capability(capabilityId, name,description, null, creationTime, requestedBy);
        await _capabilityRepository.Add(capability);
        
            // TODO [paulseghers & thfis]: check if capability already exists in db
        return capabilityId;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<KafkaTopicId> RequestNewTopic(CapabilityId capabilityId, KafkaClusterId kafkaClusterId, KafkaTopicName name,
        string description, KafkaTopicPartitions partitions, KafkaTopicRetention retention, string requestedBy)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", nameof(RequestNewTopic), this.GetType().FullName);

        if (capabilityId != name.ExtractCapabilityId())
        {
            _logger.LogError("Capability id {CapabilityId} not part of topic name {KafkaTopicName}", capabilityId, name);
            throw new Exception("Capability id not part of topic name.");
        }

        if (!await _capabilityRepository.Exists(capabilityId))
        {
            _logger.LogError("Capability with id {CapabilityId} does not exist", capabilityId);
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        if (await _kafkaTopicRepository.Exists(name))
        {
            _logger.LogError("Topic with name {KafkaTopicName} already exist", name);
            throw EntityAlreadyExistsException<Topic>.WithProperty(x => x.Name, name);
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

        _logger.LogInformation("New topic {KafkaTopicName} for capability {CapabilityId} has been requested by {RequestedBy}",
            topic.Name, capabilityId, requestedBy);

        return topic.Id;
    }
}