using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Authorization;

namespace SelfService.Domain.Services;

public class CapabilityTopicsService
{
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IKafkaClusterRepository _kafkaClusterRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Microsoft.AspNetCore.Authorization.IAuthorizationService _authorizationService;

    public CapabilityTopicsService(
        ICapabilityRepository capabilityRepository,
        IKafkaTopicRepository kafkaTopicRepository,
        IKafkaClusterRepository kafkaClusterRepository,
        IHttpContextAccessor httpContextAccessor,
        Microsoft.AspNetCore.Authorization.IAuthorizationService authorizationService)
    {
        _capabilityRepository = capabilityRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _kafkaClusterRepository = kafkaClusterRepository;
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new ApplicationException("Not in a http request context!");

    public async Task<CapabilityTopics> GetCapabilityTopics(CapabilityId capabilityId)
    {
        var capability = await GetCapability(capabilityId);
        var topics = await GetTopicsForCapabilityByMembership(capability);
        var clusters = await GetAllClusters();
        var topicsByCluster = GroupTopicsByCluster(topics, clusters);

        return new CapabilityTopics(capability, topicsByCluster.ToArray());
    }

    private async Task<Capability> GetCapability(CapabilityId capabilityId)
    {
        return await _capabilityRepository.Get(capabilityId);
    }

    private async Task<IEnumerable<KafkaTopic>> GetTopicsForCapabilityByMembership(Capability capability)
    {
        var isMemberOfCapability = await IsMemberOfCapability(capability);

        var allCapabilityTopics = await GetAllTopicsForCapability(capability);

        if (isMemberOfCapability)
        {
            return allCapabilityTopics;
        }

        // only public topics are allowed if the user is not a member of the capability
        return allCapabilityTopics.Where(x => x.IsPublic);
    }

    private async Task<bool> IsMemberOfCapability(Capability capability)
    {
        var postRequirements = new IAuthorizationRequirement[]
        {
            new IsMemberOfCapability()
        };

        var authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, capability, postRequirements);

        return authorizationResult.Succeeded;
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