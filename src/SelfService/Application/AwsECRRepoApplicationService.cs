using Amazon.ECR;
using Amazon.ECR.Model;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class AwsECRRepoApplicationService : IAwsECRRepoApplicationService
{
    public Task CreateECRRepo(AwsAccountId awsAccountId, string repoName)
    {
        AmazonECRClient client = new();

        return client.CreateRepositoryAsync(
            new CreateRepositoryRequest
            {
                ImageScanningConfiguration = new ImageScanningConfiguration() { ScanOnPush = true },
                RepositoryName = repoName,
            }
        );
    }

    public async Task<bool> HasECRRepo(string repoName)
    {
        AmazonECRClient client = new();
        var resp = await client.DescribeRepositoriesAsync(
            new DescribeRepositoriesRequest { RepositoryNames = new List<string> { repoName } }
        );

        return resp.Repositories.Any(x => x.RepositoryName == repoName);
    }
}
