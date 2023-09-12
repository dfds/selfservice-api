namespace SelfService.Application;

public interface IAwsECRRepoApplicationService
{
    Task CreateECRRepo(string repositoryName);
    Task DeleteECRRepo(string repositoryName);
    Task<List<string>> GetECRRepositories();
}
