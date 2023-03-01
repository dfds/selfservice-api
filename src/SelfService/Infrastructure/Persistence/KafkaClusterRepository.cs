using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class KafkaClusterRepository : IKafkaClusterRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public KafkaClusterRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Exists(KafkaClusterId id)
    {
        var found = await _dbContext.KafkaClusters.FindAsync(id);
        return found != null;
    }
}