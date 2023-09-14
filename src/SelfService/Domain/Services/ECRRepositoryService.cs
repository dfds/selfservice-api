using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public class ECRRepositoryService : IECRRepositoryService
{
    private readonly ILogger<ECRRepositoryService> _logger;
    private readonly IECRRepositoryRepository _ecrRepositoryRepository;
    private readonly IAwsECRRepositoryApplicationService _awsEcrRepositoryApplicationService;

    private readonly UserId _cloudEngineeringTeamUserId = UserId.Parse("cloud-engineering");

    private const string CreatedByCloudEngineeringTeamDescription =
        "Repository created automatically by the SelfService Team";

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

    [TransactionalBoundary]
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
            await _ecrRepositoryRepository.Add(newRepository);
            return newRepository;
        }
        catch (Exception e)
        {
            await _awsEcrRepositoryApplicationService.DeleteECRRepo(repositoryName);
            throw new Exception($"Error creating repo {repositoryName}: {e.Message}");
        }
    }

    [TransactionalBoundary]
    public async Task SynchronizeAwsECRAndDatabase(bool performUpdateOnMismatch)
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

        if (!performUpdateOnMismatch)
        {
            if (repositoriesNotInAws.Count > 0 || repositoriesNotInDb.Count > 0)
            {
                _logger.LogInformation(
                    "Mismatch between aws ECR repositories and database ECR repositories, but not updating because `performUpdateOnMismatch` is false"
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
                "Deleting {NumberOfDbRepositories} repositories in the database that do not exist in aws: {RepositoriesNotInAws}",
                repositoriesNotInAws.Count,
                string.Join(',', repositoriesNotInAws)
            );
            _ecrRepositoryRepository.RemoveRangeWithRepositoryName(repositoriesNotInAws);
        }

        if (repositoriesNotInDb.Count > 0)
        {
            List<ECRRepository> newRepositories = new();

            foreach (var repositoryName in repositoriesNotInDb)
            {
                newRepositories.Add(
                    new ECRRepository(
                        new ECRRepositoryId(),
                        repositoryName,
                        CreatedByCloudEngineeringTeamDescription,
                        repositoryName,
                        _cloudEngineeringTeamUserId
                    )
                );
            }

            _logger.LogInformation(
                "Adding {NumberOfAwsRepositories} repositories in aws that do not exist in the database: {RepositoriesNotInDb}",
                repositoriesNotInDb.Count,
                string.Join(',', repositoriesNotInDb)
            );
            await _ecrRepositoryRepository.AddRange(newRepositories);
        }
    }
}
