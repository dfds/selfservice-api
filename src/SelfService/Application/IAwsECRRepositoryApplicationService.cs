namespace SelfService.Application;

public interface IAwsECRRepositoryApplicationService
{
    Task<string> CreateECRRepo(string name);
    Task DeleteECRRepo(string name);
    Task<List<string>> GetECRRepositories();
}
