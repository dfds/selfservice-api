using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface ICapabilityKafkaTopicsQuery
{
    Task<IEnumerable<KafkaTopic>> FindBy(CapabilityId capabilityId);
}