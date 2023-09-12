using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public class ECRRepositoryService : IECRRepositoryService
{
    private readonly ILogger<ECRRepositoryService> _logger;
    private readonly IECRRepositoryRepository _ecrRepositoryRepository;
    private readonly IAwsECRRepositoryApplicationService _awsEcrRepositoryApplicationService;

    public ECRRepositoryService(
        ILogger<ECRRepositoryService> logger,
        IECRRepositoryRepository ecrRepositoryRepository,
        IAwsECRRepositoryApplicationService awsEcrRepositoryApplicationService
    )
    {
        _logger = logger;
        _ecrRepositoryRepository = ecrRepositoryRepository;
        _awsEcrRepositoryApplicationService = awsEcrRepositoryApplicationService;
    }

    public Task<IEnumerable<ECRRepository>> GetAllECRRepositories()
    {
        return _ecrRepositoryRepository.GetAll();
    }

    public async Task<ECRRepository> AddRepository(
        string name,
        string description,
        string repositoryName,
        UserId userId
    )
    {
        try
        {
            _logger.LogInformation("Adding new ECRRepository in aws: {ECRRepositoryName}", repositoryName);
            await _awsEcrRepositoryApplicationService.CreateECRRepo(repositoryName);
            var newRepository = new ECRRepository(new ECRRepositoryId(), name, description, repositoryName, userId);
            _logger.LogInformation("Adding new ECRRepository to the database: {ECRRepositoryName}", repositoryName);
            _ecrRepositoryRepository.Add(newRepository);
            return newRepository;
        }
        catch (Exception e)
        {
            await _awsEcrRepositoryApplicationService.DeleteECRRepo(repositoryName);
            throw new Exception($"Error creating repo {repositoryName}: {e.Message}");
        }
    }
}
