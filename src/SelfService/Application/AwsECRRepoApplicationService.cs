using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;

namespace SelfService.Application;

public class AwsECRRepoApplicationService : IAwsECRRepoApplicationService
{
    public Task CreateECRRepo(string repoName)
    {
        AmazonECRClient client = new(new AmazonECRConfig { RegionEndpoint = RegionEndpoint.EUCentral1, });

        return client.CreateRepositoryAsync(
            new CreateRepositoryRequest
            {
                ImageScanningConfiguration = new ImageScanningConfiguration() { ScanOnPush = true },
                RepositoryName = repoName,
            }
        );
    }
}
