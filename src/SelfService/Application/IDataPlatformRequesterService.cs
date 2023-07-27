namespace SelfService.Application;

public interface IDataPlatformRequesterService
{
    Task<IEnumerable<string>> GetConsumersForKafkaTopic(string capability);
}