using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsECRRepoApplicationService
{
    Task CreateECRRepo(string repoName);
}
