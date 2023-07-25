using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IKafkaTopicConsumerService
{
    Task<IEnumerable<string>> GetConsumersForKafkaTopic(string name);
}
