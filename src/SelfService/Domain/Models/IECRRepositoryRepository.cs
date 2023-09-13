namespace SelfService.Domain.Models;

public interface IECRRepositoryRepository
{
    Task<IEnumerable<ECRRepository>> GetAll();
    Task Add(ECRRepository ecrRepository);
    Task AddRange(List<ECRRepository> ecrRepositories);
    void RemoveRangeWithRepositoryName(List<string> repositoryNames);
}
