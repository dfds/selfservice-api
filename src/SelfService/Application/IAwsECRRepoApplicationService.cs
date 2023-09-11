using Amazon.ECR.Model;
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsECRRepoApplicationService
{
    Task CreateECRRepo(string repositoryName);
    Task DeleteECRRepo(string repositoryName);
}
