using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IECRRepositoryService
{
    Task<bool> HasRepository(string repositoryName);
    Task<IEnumerable<ECRRepository>> GetAllECRRepositories();
    Task<ECRRepository> AddRepository(string name, string description, UserId userId);
    Task SynchronizeAwsECRAndDatabase(bool performUpdateOnMismatch = false);
}
