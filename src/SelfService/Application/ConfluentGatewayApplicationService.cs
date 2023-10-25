using Confluent.Kafka.Admin;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Application;

public class ConfluentGatewayApplicationService
{
    private readonly IConfluentCloudClientService _confluentCloudClientService;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;

    public ConfluentGatewayApplicationService(
        IConfluentCloudClientService confluentCloudClientService,
        IKafkaTopicRepository kafkaTopicRepository
    )
    {
        _confluentCloudClientService = confluentCloudClientService;
        _kafkaTopicRepository = kafkaTopicRepository;
    }

    public void CreateTopic() { }

    public void DeleteTopic() { }

    public void RegisterTopicSchema() { }

    public async Task RequestClusterAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId)
    {
        var serviceAccount = await _confluentCloudClientService.CreateServiceAccount(
            capabilityId.ToString(),
            kafkaClusterId.ToString()
        );

        var acls = ConfluentCloudAclHelper.GetAcls(capabilityId, serviceAccount);
        await _confluentCloudClientService.CreateAclEntries(kafkaClusterId, acls);
    }
}
