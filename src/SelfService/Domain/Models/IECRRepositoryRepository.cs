using SelfService.Infrastructure.Persistence;

namespace SelfService.Domain.Models;

public interface IECRRepositoryRepository
{
    IEnumerable<ECRRepositoryRepository> GetAll();
    void Add(ECRRepositoryRepository ecr);
}
