using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public class CapabilityTopicsService
{
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IKafkaClusterRepository _kafkaClusterRepository;

    public CapabilityTopicsService(IKafkaTopicRepository kafkaTopicRepository, IKafkaClusterRepository kafkaClusterRepository)
    {
        _kafkaTopicRepository = kafkaTopicRepository;
        _kafkaClusterRepository = kafkaClusterRepository;
    }

    public async Task<IEnumerable<CapabilityTopics>> GetCapabilityTopics(CapabilityId capabilityId, bool publicOnly)
    {
        var allCapabilityTopics = await _kafkaTopicRepository.FindBy(capabilityId);

        if (publicOnly)
        {
            // only public topics are allowed if the user only has read access
            allCapabilityTopics = allCapabilityTopics.Where(x => x.IsPublic);
        }

        var allClusters = await _kafkaClusterRepository.GetAll();

        var clusterTopics = allCapabilityTopics.ToLookup(x => x.KafkaClusterId);

        return allClusters
            .Select(cluster => new CapabilityTopics(cluster, clusterTopics[cluster.Id].ToArray()));
    }
}