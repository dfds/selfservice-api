using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IECRRepositoryService
{
    Task<IEnumerable<ECRRepository>> GetAllECRRepositories();
    void AddRepository(ECRRepository repository);
}
