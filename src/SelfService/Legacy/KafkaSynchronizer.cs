using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Legacy.Models;

namespace SelfService.Legacy;

public class KafkaSynchronizer
{
    private readonly ILogger<KafkaSynchronizer> _logger;
    private readonly LegacyDbContext _legacyDbContext;
    private readonly SelfServiceDbContext _selfServiceDbContext;

    public KafkaSynchronizer(ILogger<KafkaSynchronizer> logger, LegacyDbContext legacyDbContext, SelfServiceDbContext selfServiceDbContext)
    {
        _logger = logger;
        _legacyDbContext = legacyDbContext;
        _selfServiceDbContext = selfServiceDbContext;
    }

    public async Task Synchronize(CancellationToken stoppingToken)
    {
        await SynchronizeClusters(stoppingToken);
        await SynchronizeTopics(stoppingToken);
    }

    private async Task SynchronizeClusters(CancellationToken stoppingToken)
    {
        var legacyClusters = await GetAllLegacyClusters(stoppingToken);
        var clusters = await GetAllClusters(stoppingToken);

        _logger.LogInformation("Legacy Clusters {Count}", legacyClusters.Count);
        _logger.LogInformation("Clusters {Count}", clusters.Count);

        foreach (var legacyCluster in legacyClusters)
        {
            var cluster = clusters.FirstOrDefault(x => x.Id == legacyCluster.ClusterId);
            if (cluster == null)
            {
                var kafkaCluster = new KafkaCluster(
                    id: KafkaClusterId.Parse(legacyCluster.ClusterId),
                    name: legacyCluster.Name ?? "",
                    description: legacyCluster.Description ?? "",
                    enabled: legacyCluster.Enabled
                );

                await _selfServiceDbContext.KafkaClusters.AddAsync(kafkaCluster, stoppingToken);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(legacyCluster.Name))
                {
                    cluster.Name = legacyCluster.Name;
                }

                if (!string.IsNullOrWhiteSpace(legacyCluster.Description))
                {
                    cluster.Description = legacyCluster.Description;
                }

                cluster.Enabled = legacyCluster.Enabled;
            }
        }
    }

    private Task<List<Cluster>> GetAllLegacyClusters(CancellationToken stoppingToken)
    {
        return _legacyDbContext.Clusters.ToListAsync(stoppingToken);
    }

    private Task<List<KafkaCluster>> GetAllClusters(CancellationToken stoppingToken)
    {
        return _selfServiceDbContext.KafkaClusters.ToListAsync(stoppingToken);
    }

    private async Task SynchronizeTopics(CancellationToken stoppingToken)
    {
        var legacyClusters = await GetAllLegacyClusters(stoppingToken);
        var legacyTopics = await GetAllLegacyTopics(stoppingToken);
        var topics = await GetAllTopics(stoppingToken);

        _logger.LogInformation("Legacy Topics {Count}", legacyTopics.Count);
        _logger.LogInformation("Topics {Count}", topics.Count);

        foreach (var legacyTopic in legacyTopics)
        {
            var topic = topics.FirstOrDefault(x => x.Id == legacyTopic.Id);
            if (topic == null)
            {
                var cluster = legacyClusters.Single(x => x.Id == legacyTopic.KafkaClusterId);

                topic = new KafkaTopic
                (
                    id: legacyTopic.Id,
                    capabilityId: legacyTopic.CapabilityId,
                    kafkaClusterId: KafkaClusterId.Parse(cluster.ClusterId), 
                    name: legacyTopic.Name,
                    description: legacyTopic.Description,
                    status: legacyTopic.Status switch
                    {
                        "InProgress" => KafkaTopicStatusType.InProgress,
                        "In Progress" => KafkaTopicStatusType.InProgress,
                        "inprogress" => KafkaTopicStatusType.InProgress,
                        "in progress" => KafkaTopicStatusType.InProgress,
                        "Provisioned" => KafkaTopicStatusType.Provisioned,
                        "provisioned" => KafkaTopicStatusType.Provisioned,
                        _ => KafkaTopicStatusType.Unknown
                    },
                    partitions: Convert.ToUInt32(legacyTopic.Partitions),
                    retention: legacyTopic.Retention,
                    createdAt: DateTime.SpecifyKind(legacyTopic.Created, DateTimeKind.Utc),
                    createdBy: "LEGACY SYNCHRONIZER",
                    modifiedAt: legacyTopic.LastModified.HasValue
                        ? DateTime.SpecifyKind(legacyTopic.LastModified.Value, DateTimeKind.Utc)
                        : DateTime.UtcNow,
                    modifiedBy: "LEGACY SYNCHRONIZER"
                );

                await _selfServiceDbContext.KafkaTopics.AddAsync(topic, stoppingToken);
            }
            else
            {
                topic.ChangeDescription(
                    newDescription: legacyTopic.Description,
                    modifiedAt: legacyTopic.LastModified.HasValue 
                        ? DateTime.SpecifyKind(legacyTopic.LastModified.Value, DateTimeKind.Utc) 
                        : DateTime.UtcNow,
                    modifiedBy: "LEGACY SYNCHRONIZER"
                );
            }
        }

        await _selfServiceDbContext.SaveChangesAsync(stoppingToken);
    }

    private Task<List<Topic>> GetAllLegacyTopics(CancellationToken stoppingToken)
    {
        return _legacyDbContext.Topics.FromSqlRaw(@"
select t.""Id"",
       c.""RootId"" as ""CapabilityId"",
       t.""KafkaClusterId"",
       t.""Name"",
       t.""Description"",
       t.""Status"",
       t.""Partitions"",
       ((t.""Configurations""::json) ->> 'retention.ms')::bigint as ""Retention"",
       t.""Created"",
       t.""LastModified""
from ""KafkaTopic"" t
inner join ""Capability"" c on t.""CapabilityId"" = c.""Id"" and length(c.""RootId"")>0
").ToListAsync(stoppingToken);
    }

    private Task<List<KafkaTopic>> GetAllTopics(CancellationToken stoppingToken)
    {
        return _selfServiceDbContext.KafkaTopics.ToListAsync(stoppingToken);
    }
}