using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IECRRepositoryService
{
    Task<IEnumerable<ECRRepository>> GetAllECRRepositories();
    Task<ECRRepository> AddRepository(string name, string description, string repositoryName, UserId userId);
}
