namespace SelfService.Application;

public interface IAwsECRRepositoryApplicationService
{
    Task CreateECRRepo(string repositoryName);
    Task DeleteECRRepo(string repositoryName);
    Task<List<string>> GetECRRepositories();
}
