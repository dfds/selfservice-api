using SelfService.Domain.Models;

namespace SelfService.Application;

public class ECRRepositoryService : IECRRepositoryService
{
    private IECRRepositoryRepository _ecrRepositoryRepository;
    private IAwsECRRepoApplicationService _awsECRRepoApplicationService;
    
    public ECRRepositoryService(IECRRepositoryRepository ecrRepositoryRepository, IAwsECRRepoApplicationService awsECRRepoApplicationService)
    {
        _ecrRepositoryRepository = ecrRepositoryRepository;
        _awsECRRepoApplicationService = awsECRRepoApplicationService;
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