using Prometheus;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Metrics;

namespace SelfService.Domain.Services;

public class ECRRepositoryService : IECRRepositoryService
{
    private readonly ILogger<ECRRepositoryService> _logger;
    private readonly IECRRepositoryRepository _ecrRepositoryRepository;
    private readonly IAwsECRRepositoryApplicationService _awsEcrRepositoryApplicationService;
    private SystemTime _systemTime;

    private readonly UserId _cloudEngineeringTeamUserId = UserId.Parse("cloud-engineering");

    private const string CreatedByCloudEngineeringTeamDescription =
        "Repository created automatically by the SelfService Team";

    private readonly bool _localDevSkipAwsECRRepositoryCreation = false;
    private readonly OutOfSyncECRInfo _outOfSyncEcrInfo;

    public ECRRepositoryService(
        ILogger<ECRRepositoryService> logger,
        IECRRepositoryRepository ecrRepositoryRepository,
        IAwsECRRepositoryApplicationService awsEcrRepositoryApplicationService,
        SystemTime systemTime,
        OutOfSyncECRInfo outOfSyncEcrInfo
    )
    {
        _logger = logger;
        _ecrRepositoryRepository = ecrRepositoryRepository;
        _awsEcrRepositoryApplicationService = awsEcrRepositoryApplicationService;
        _systemTime = systemTime;
        var envValue = Environment.GetEnvironmentVariable("LOCAL_DEV_SKIP_AWS_ECR_REPOSITORY_CREATION") ?? "false";
        _localDevSkipAwsECRRepositoryCreation = envValue == "true";
        _outOfSyncEcrInfo = outOfSyncEcrInfo;
    }

    public Task<bool> HasRepository(string repositoryName)
    {
        return _ecrRepositoryRepository.HasRepository(repositoryName);
    }

    public Task<IEnumerable<ECRRepository>> GetAllECRRepositories()
    {
        return _ecrRepositoryRepository.GetAll();
    }

    public async Task<OutOfSyncECRInfo> GetOutofSyncECRCount()
    {
        var awsRepositoriesSet = new HashSet<string>(await _awsEcrRepositoryApplicationService.GetECRRepositories());

        var repositoriesInDb = await _ecrRepositoryRepository.GetAll();
        var dbRepositoriesSet = new HashSet<string>();
        foreach (var ecrRepository in repositoriesInDb)
        {
            dbRepositoriesSet.Add(ecrRepository.Name);
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

        int repositoriesNotInAwsCount = repositoriesNotInAws.Count;
        int repositoriesNotInDbCount = repositoriesNotInDb.Count;

        var outOfSyncECRInfo = new OutOfSyncECRInfo(
            repositoriesNotInAwsCount,
            repositoriesNotInDbCount,
            repositoriesNotInAws,
            repositoriesNotInDb,
            true, // this value will only be used in updates to the in-memory OutOfSyncECRInfo Singleton
            DateTime.Now.AddHours(12) //12h = 720min
        );

        return outOfSyncECRInfo;
    }

    public async Task UpdateOutOfSyncECRInfo()
    {
        var outOfSyncEcrInfo = await GetOutofSyncECRCount();
        _outOfSyncEcrInfo.SetValuesFromInstance(outOfSyncEcrInfo);
        EcrMetrics.OutOfSyncEcrMetric.Set(
            _outOfSyncEcrInfo.RepositoriesNotInAwsCount + _outOfSyncEcrInfo.RepositoriesNotInDbCount
        );
    }

    private string getUri(string name)
    {
        return $"579478677147.dkr.ecr.eu-central-1.amazonaws.com/{name}";
    }

    [TransactionalBoundary]
    public async Task<ECRRepository> AddRepository(string name, string description, UserId userId)
    {
        try
        {
            if (_localDevSkipAwsECRRepositoryCreation)
            {
                _logger.LogInformation("Skipping AWS calls because LOCAL_DEV_SKIP_AWS_CALLS is true");
            }
            else
            {
                _logger.LogInformation("Adding new ECRRepository in aws: {ECRRepositoryName}", name);
                await _awsEcrRepositoryApplicationService.CreateECRRepo(name);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error creating repo {name}: {e.Message}");
            await _awsEcrRepositoryApplicationService.DeleteECRRepo(name);
            throw new Exception($"Error creating repo {name}: {e.Message}");
        }

        var uri = getUri(name);
        var newRepository = new ECRRepository(new ECRRepositoryId(), name, description, uri, userId, DateTime.UtcNow);
        _logger.LogInformation("Adding new ECRRepository to the database: {ECRRepositoryName}", name);
        await _ecrRepositoryRepository.Add(newRepository);
        return newRepository;
    }

    [TransactionalBoundary]
    public async Task SynchronizeAwsECRAndDatabase(bool performUpdateOnMismatch)
    {
        var outOfSyncEcrInfo = await GetOutofSyncECRCount();

        var repositoriesNotInAws = outOfSyncEcrInfo.RepositoriesNotInAws;
        var repositoriesNotInDb = outOfSyncEcrInfo.RepositoriesNotInDb;

        int repositoriesNotInAwsCount = outOfSyncEcrInfo.RepositoriesNotInAwsCount;
        int repositoriesNotInDbCount = outOfSyncEcrInfo.RepositoriesNotInDbCount;

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
                    repositoriesNotInAwsCount,
                    string.Join(',', repositoriesNotInAws)
                );
            }

            if (repositoriesNotInDb.Count > 0)
            {
                _logger.LogWarning(
                    "Found {NumberOfAwsRepositories} repositories in aws that do not exist in the database: {RepositoriesNotInDb}",
                    repositoriesNotInDbCount,
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

            foreach (var name in repositoriesNotInDb)
            {
                var uri = getUri(name);
                newRepositories.Add(
                    new ECRRepository(
                        new ECRRepositoryId(),
                        name,
                        CreatedByCloudEngineeringTeamDescription,
                        uri,
                        _cloudEngineeringTeamUserId,
                        null
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
