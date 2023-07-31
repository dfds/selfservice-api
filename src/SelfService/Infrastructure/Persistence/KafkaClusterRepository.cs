using Microsoft.EntityFrameworkCore;
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

    public async Task<KafkaCluster?> FindBy(KafkaClusterId id)
    {
        return await _dbContext.KafkaClusters.Where(x => x.Enabled).SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<KafkaCluster>> GetAll()
    {
        return await _dbContext.KafkaClusters.Where(x => x.Enabled).OrderBy(x => x.Name).ToListAsync();
    }
}
