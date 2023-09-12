using SelfService.Domain.Models;

namespace SelfService.Application;

public class ECRRepositoryService : IECRRepositoryService
{
    private IECRRepositoryRepository _ecrRepositoryRepository;

    public ECRRepositoryService(IECRRepositoryRepository ecrRepositoryRepository)
    {
        _ecrRepositoryRepository = ecrRepositoryRepository;
    }

    public Task<IEnumerable<ECRRepository>> GetAllECRRepositories()
    {
        return _ecrRepositoryRepository.GetAll();
    }

    /// <summary>
    /// Add new ECRRepository to the database
    /// </summary>
    public void AddRepository(ECRRepository repository)
    {
        _ecrRepositoryRepository.Add(repository);
    }
}
