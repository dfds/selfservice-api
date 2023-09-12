namespace SelfService.Domain.Models;

public interface IECRRepositoryRepository
{
    Task<IEnumerable<ECRRepository>> GetAll();
    void Add(ECRRepository ecr);
}
