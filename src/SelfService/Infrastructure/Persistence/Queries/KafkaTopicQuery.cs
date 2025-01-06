using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence.Queries;

public class KafkaTopicQuery : IKafkaTopicQuery
{
    private readonly ILogger<KafkaTopicQuery> _logger;
    private readonly SelfServiceDbContext _dbContext;

    public KafkaTopicQuery(ILogger<KafkaTopicQuery> logger, SelfServiceDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<KafkaTopic>> Query(KafkaTopicQueryParams queryParams, UserId userId)
    {
        var sql = queryParams switch
        {
            { CapabilityId: null, IncludePrivate: true } => GenerateSqlQueryForAllTopicsAccessibleForUser(userId),
            { CapabilityId: null, IncludePrivate: not true } => GenerateSqlQueryForAllPublicTopics(),
            { CapabilityId: not null, IncludePrivate: true } => GenerateSqlQueryForAllTopicsWithCapability(
                userId,
                queryParams.CapabilityId
            ),
            { CapabilityId: not null, IncludePrivate: not true } => GenerateSqlQueryForPublicTopicsWithCapability(
                queryParams.CapabilityId
            ),
        };

        var query = _dbContext.KafkaTopics.FromSql(sql);

        if (queryParams.ClusterId is not null)
        {
            query = query.Where(x => x.KafkaClusterId == queryParams.ClusterId);
        }

        var kafkaTopics = await query.OrderBy(x => x.Name).AsNoTracking().ToListAsync();

        _logger.LogDebug("Found {Count} Kafka Topics for query {@QueryParams}", kafkaTopics.Count, queryParams);

        return kafkaTopics;
    }

    private static FormattableString GenerateSqlQueryForAllTopicsAccessibleForUser(string userId)
    {
        return $@"
select * from ""KafkaTopic""
where ""Name"" ilike 'pub.%'
or ""CapabilityId"" in (
    select m.""CapabilityId"" from ""Membership"" m
    where m.""UserId"" = {userId}
)";
    }

    private static FormattableString GenerateSqlQueryForAllPublicTopics()
    {
        return $@"
select * from ""KafkaTopic""
where ""Name"" ilike 'pub.%'";
    }

    private static FormattableString GenerateSqlQueryForAllTopicsWithCapability(string userId, string capabilityId)
    {
        return $@"
select * from ""KafkaTopic""
where ""CapabilityId"" in (
    select distinct m.""CapabilityId"" from ""Membership"" m
    where m.""UserId"" = {userId}
    and m.""CapabilityId"" = {capabilityId}
)
or ""Name"" ilike 'pub.%'
and ""CapabilityId"" = {capabilityId}";
    }

    private static FormattableString GenerateSqlQueryForPublicTopicsWithCapability(string capabilityId)
    {
        return $@"
select * from ""KafkaTopic""
where ""Name"" ilike 'pub.%'
and ""CapabilityId"" = {capabilityId}";
    }
}
