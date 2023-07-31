using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubKafkaClusterRepository : IKafkaClusterRepository
{
    private readonly KafkaCluster[] _clusters;

    public StubKafkaClusterRepository(params KafkaCluster[] clusters)
    {
        _clusters = clusters;
    }

    public Task<bool> Exists(KafkaClusterId id)
    {
        throw new NotImplementedException();
    }

    public Task<KafkaCluster?> FindBy(KafkaClusterId id)
    {
        return Task.FromResult(_clusters.SingleOrDefault());
    }

    public Task<IEnumerable<KafkaCluster>> GetAll()
    {
        return Task.FromResult(_clusters.AsEnumerable());
    }
}
