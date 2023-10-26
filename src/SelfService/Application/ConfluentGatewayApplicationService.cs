using Confluent.Kafka.Admin;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Application;

public class ConfluentGatewayApplicationService
{
    private readonly IConfluentCloudClientService _confluentCloudClientService;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IKafkaClusterAccessRepository _kafkaClusterAccessRepository;

    public ConfluentGatewayApplicationService(
        IConfluentCloudClientService confluentCloudClientService,
        IKafkaTopicRepository kafkaTopicRepository,
        IKafkaClusterAccessRepository kafkaClusterAccessRepository
    )
    {
        _confluentCloudClientService = confluentCloudClientService;
        _kafkaTopicRepository = kafkaTopicRepository;
        _kafkaClusterAccessRepository = kafkaClusterAccessRepository;
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

        // Has cluster access?
        var clusterAccess = await _kafkaClusterAccessRepository.FindBy(capabilityId, kafkaClusterId);
        var apiKey = await _confluentCloudClientService.CreateApiKey(
            serviceAccount,
            kafkaClusterId.ToString(),
            "SelfService"
        );
    }
}
