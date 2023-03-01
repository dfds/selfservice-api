namespace SelfService.Domain.Models;

public interface IKafkaClusterRepository
{
    Task<bool> Exists(KafkaClusterId id);
}