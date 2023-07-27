namespace SelfService.Domain.Models;

public interface IKafkaClusterRepository
{
    Task<bool> Exists(KafkaClusterId id);
    Task<KafkaCluster?> FindBy(KafkaClusterId id);
    Task<IEnumerable<KafkaCluster>> GetAll();
}