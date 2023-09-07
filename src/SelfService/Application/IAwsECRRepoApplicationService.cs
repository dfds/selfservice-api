using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsECRRepoApplicationService
{
    Task CreateECRRepo(AwsAccountId awsAccountId, string repoName);
}
