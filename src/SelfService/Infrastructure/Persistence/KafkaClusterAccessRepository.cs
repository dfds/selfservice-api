using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class KafkaClusterAccessRepository : IKafkaClusterAccessRepository
{
    private readonly SelfServiceDbContext _context;

    public KafkaClusterAccessRepository(SelfServiceDbContext context)
    {
        _context = context;
    }

    public async Task Add(KafkaClusterAccess kafkaClusterAccess)
    {
        await _context.KafkaClusterAccess.AddAsync(kafkaClusterAccess);
    }

    public async Task<KafkaClusterAccess?> FindBy(CapabilityId capabilityId, KafkaClusterId kafkaClusterId)
    {
        var kafkaClusterAccess = await _context.KafkaClusterAccess.SingleOrDefaultAsync(
            x => x.CapabilityId == capabilityId && x.KafkaClusterId == kafkaClusterId
        );

        return kafkaClusterAccess;
    }
}
