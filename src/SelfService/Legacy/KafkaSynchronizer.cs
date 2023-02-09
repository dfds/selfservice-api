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
            var cluster = clusters.FirstOrDefault(x => x.Id == legacyCluster.Id);
            if (cluster == null)
            {
                var kafkaCluster = new KafkaCluster
                {
                    Id = legacyCluster.Id,
                    ClusterId = legacyCluster.ClusterId,
                    Name = legacyCluster.Name,
                    Description = legacyCluster.Description,
                    Enabled = legacyCluster.Enabled
                };
                await _selfServiceDbContext.KafkaClusters.AddAsync(kafkaCluster, stoppingToken);
            }
            else
            {
                cluster.Id = legacyCluster.Id;
                cluster.ClusterId = legacyCluster.ClusterId;
                cluster.Name = legacyCluster.Name;
                cluster.Description = legacyCluster.Description;
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
        var legacyTopics = await GetAllLegacyTopics(stoppingToken);
        var topics = await GetAllTopics(stoppingToken);

        _logger.LogInformation("Legacy Topics {Count}", legacyTopics.Count);
        _logger.LogInformation("Topics {Count}", topics.Count);

        foreach (var legacyTopic in legacyTopics)
        {
            var topic = topics.FirstOrDefault(x => x.Id == legacyTopic.Id);
            if (topic == null)
            {
                topic = new KafkaTopic
                {
                    Id = legacyTopic.Id,
                    CapabilityId = legacyTopic.CapabilityId,
                    KafkaClusterId = legacyTopic.KafkaClusterId,
                    Name = legacyTopic.Name,
                    Description = legacyTopic.Description,
                    Status = legacyTopic.Status,
                    Partitions = legacyTopic.Partitions,
                    Retention = legacyTopic.Retention,
                    CreatedAt = DateTime.SpecifyKind(legacyTopic.Created, DateTimeKind.Utc),
                    CreatedBy = "SYSTEM",
                    ModifiedAt =  legacyTopic.LastModified.HasValue ? DateTime.SpecifyKind(legacyTopic.LastModified.Value, DateTimeKind.Utc) : null,
                    ModifiedBy = legacyTopic.LastModified.HasValue ? "SYSTEM" : null
                };
                await _selfServiceDbContext.KafkaTopics.AddAsync(topic, stoppingToken);
            }
            else
            {
                topic.CapabilityId = legacyTopic.CapabilityId;
                topic.KafkaClusterId = legacyTopic.KafkaClusterId;
                topic.Name = legacyTopic.Name;
                topic.Description = legacyTopic.Description;
                topic.Status = legacyTopic.Status;
                topic.Partitions = legacyTopic.Partitions;
                topic.Retention = legacyTopic.Retention;
                topic.ModifiedAt =  legacyTopic.LastModified.HasValue ? DateTime.SpecifyKind(legacyTopic.LastModified.Value, DateTimeKind.Utc) : null;
                topic.ModifiedBy = legacyTopic.LastModified.HasValue ? "SYSTEM" : null;
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