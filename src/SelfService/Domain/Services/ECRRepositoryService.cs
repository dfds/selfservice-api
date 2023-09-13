using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public class ECRRepositoryService : IECRRepositoryService
{
    private readonly ILogger<ECRRepositoryService> _logger;
    private readonly IECRRepositoryRepository _ecrRepositoryRepository;
    private readonly IAwsECRRepositoryApplicationService _awsEcrRepositoryApplicationService;

    private readonly bool _updateRepositoriesOnStateMismatch;

    public ECRRepositoryService(
        ILogger<ECRRepositoryService> logger,
        IECRRepositoryRepository ecrRepositoryRepository,
        IAwsECRRepositoryApplicationService awsEcrRepositoryApplicationService
    )
    {
        _logger = logger;
        _ecrRepositoryRepository = ecrRepositoryRepository;
        _awsEcrRepositoryApplicationService = awsEcrRepositoryApplicationService;
        _updateRepositoriesOnStateMismatch =
            Environment.GetEnvironmentVariable("UPDATE_REPOSITORIES_ON_STATE_MISMATCH") == "true";
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

    public async Task SynchronizeAwsECRAndDatabase()
    {
        var awsRepositoriesSet = new HashSet<string>(await _awsEcrRepositoryApplicationService.GetECRRepositories());

        var repositoriesInDb = await _ecrRepositoryRepository.GetAll();
        var dbRepositoriesSet = new HashSet<string>();
        foreach (var ecrRepository in repositoriesInDb)
        {
            dbRepositoriesSet.Add(ecrRepository.RepositoryName);
        }

        // Find repositories that exist in the database but not in aws
        List<string> repositoriesNotInAws = new();
        dbRepositoriesSet
            .ToList()
            .ForEach(repo =>
            {
                if (!awsRepositoriesSet.Contains(repo))
                {
                    repositoriesNotInAws.Add(repo);
                }
            });

        // Find repositories that exist in aws but not in the database
        List<string> repositoriesNotInDb = new();
        awsRepositoriesSet
            .ToList()
            .ForEach(repo =>
            {
                if (!dbRepositoriesSet.Contains(repo))
                {
                    repositoriesNotInDb.Add(repo);
                }
            });

        if (!_updateRepositoriesOnStateMismatch)
        {
            if (repositoriesNotInAws.Count > 0 || repositoriesNotInDb.Count > 0)
            {
                _logger.LogInformation(
                    "Mismatch between aws ECR repositories and database ECR repositories, but not updating because UPDATE_REPOSITORIES_ON_STATE_MISMATCH is not set to true"
                );
            }

            if (repositoriesNotInAws.Count > 0)
            {
                _logger.LogError(
                    "Found {NumberOfDbRepositories} repositories in the database that do not exist in aws: {RepositoriesNotInAws}",
                    repositoriesNotInAws.Count,
                    string.Join(',', repositoriesNotInAws)
                );
            }

            if (repositoriesNotInDb.Count > 0)
            {
                _logger.LogWarning(
                    "Found {NumberOfAwsRepositories} repositories in aws that do not exist in the database: {RepositoriesNotInDb}",
                    repositoriesNotInDb.Count,
                    string.Join(',', repositoriesNotInDb)
                );
            }

            return;
        }

        // Path: Updating enabled
        // 1. Delete repositories that exist in the database but not in aws
        // 2. Add repositories that exist in aws but not in the database
        if (repositoriesNotInAws.Count > 0)
        {
            _logger.LogWarning(
                "Deleting {NumberOfDbRepositories} repositories from the database that do not exist in aws",
                repositoriesNotInAws.Count
            );
            foreach (var repositoryName in repositoriesNotInAws)
            {
                try
                {
                    _logger.LogInformation("Deleting ECRRepository {ECRRepositoryName} from database", repositoryName);
                    await _ecrRepositoryRepository.RemoveWithRepositoryName(repositoryName);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "Error deleting ECRRepository {ECRRepositoryName} from database: {Exception} ",
                        repositoryName,
                        e.Message
                    );
                }
            }
        }

        if (repositoriesNotInDb.Count > 0)
        {
            _logger.LogWarning(
                "Adding {NumberOfAwsRepositories} repositories to the database that do not exist in the database",
                repositoriesNotInDb.Count
            );
            var cloudEngineeringUserId = UserId.Parse("cloud-engineering");
            foreach (var repositoryName in repositoriesNotInDb)
            {
                _logger.LogInformation("Adding ECRRepository {ECRRepositoryName} to the database", repositoryName);
                _ecrRepositoryRepository.Add(
                    new ECRRepository(
                        new ECRRepositoryId(),
                        repositoryName,
                        "Repository created automatically by the SelfService Team",
                        repositoryName,
                        cloudEngineeringUserId
                    )
                );
            }
        }
    }
}
