using SelfService.Infrastructure.Persistence;

namespace SelfService.Domain.Models;

public interface IECRRepositoryRepository
{
    IEnumerable<ECRRepository> GetAll();
    void Add(ECRRepository ecr);
}
