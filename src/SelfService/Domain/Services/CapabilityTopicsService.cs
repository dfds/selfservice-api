using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class CapabilityTopicsService // NOTE [jandr@2023-05-31]: this is not a domain service - seems more like a query...?
{
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IKafkaClusterRepository _kafkaClusterRepository;
    private readonly IMembershipQuery _membershipQuery;

    public CapabilityTopicsService(
        ICapabilityRepository capabilityRepository,
        IKafkaTopicRepository kafkaTopicRepository,
        IKafkaClusterRepository kafkaClusterRepository,
        IMembershipQuery membershipQuery)
    {
        _capabilityRepository = capabilityRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _kafkaClusterRepository = kafkaClusterRepository;
        _membershipQuery = membershipQuery;
    }

    public async Task<CapabilityTopics> GetCapabilityTopics(UserId userId, CapabilityId capabilityId)
    {
        var capability = await GetCapability(capabilityId);
        var topics = await GetTopicsForCapabilityByMembership(userId, capability);
        var clusters = await GetAllClusters();
        var topicsByCluster = GroupTopicsByCluster(topics, clusters);

        return new CapabilityTopics(capability, topicsByCluster.ToArray());
    }

    private async Task<Capability> GetCapability(CapabilityId capabilityId)
    {
        return await _capabilityRepository.Get(capabilityId);
    }

    private async Task<IEnumerable<KafkaTopic>> GetTopicsForCapabilityByMembership(UserId userId, Capability capability)
    {
        var allCapabilityTopics = await GetAllTopicsForCapability(capability);

        if (await IsMemberOfCapability(userId, capability))
        {
            return allCapabilityTopics;
        }

        // only public topics are allowed if the user is not a member of the capability
        return allCapabilityTopics.Where(x => x.IsPublic);
    }

    private async Task<bool> IsMemberOfCapability(UserId userId, Capability capability)
    {
        return await _membershipQuery.HasActiveMembership(userId, capability.Id);
    }

    private async Task<IEnumerable<KafkaTopic>> GetAllTopicsForCapability(Capability capability)
    {
        return await _kafkaTopicRepository.FindBy(capability.Id);
    }

    private async Task<IEnumerable<KafkaCluster>> GetAllClusters()
    {
        return await _kafkaClusterRepository.GetAll();
    }

    private static IEnumerable<ClusterTopics> GroupTopicsByCluster(IEnumerable<KafkaTopic> topics, IEnumerable<KafkaCluster> clusters)
    {
        var topicLookup = topics.ToLookup(x => x.KafkaClusterId);

        foreach (var cluster in clusters)
        {
            yield return new ClusterTopics(cluster, topicLookup[cluster.Id].ToArray());
        }
    }
}