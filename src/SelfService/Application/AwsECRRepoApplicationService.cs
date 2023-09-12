using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;

namespace SelfService.Application;

public class AwsECRRepoApplicationService : IAwsECRRepoApplicationService
{
    private string GetPermissionJson(string awsAccountId)
    {
        return $$"""
                 {
                   "Version": "2012-10-17",
                   "Statement": [
                     {
                       "Sid": "Allow pull from all",
                       "Effect": "Allow",
                       "Principal": {
                         "AWS": [
                           "arn:aws:iam::{{awsAccountId}}:root"
                         ]
                       },
                       "Action": [
                         "ecr:BatchCheckLayerAvailability",
                         "ecr:BatchGetImage",
                         "ecr:GetDownloadUrlForLayer"
                       ]
                     }
                   ]
                 }
                 """;
    }

    /// <summary>
    /// 1. We create the repo
    /// 2. We set the policy to allow the account to pull from the repo
    ///
    /// In case of failure setting the policy we delete the repo to not have orphaned repos
    /// </summary>
    public async Task CreateECRRepo(string repoName)
    {
        AmazonECRClient client = new(new AmazonECRConfig { RegionEndpoint = RegionEndpoint.EUCentral1, });
        var accountId = Environment.GetEnvironmentVariable("ECR_PULL_PERMISSION_AWS_ACCOUNT_ID");
        if (accountId == null)
            throw new Exception("ECR_PULL_PERMISSION_AWS_ACCOUNT_ID environment variable is not set");

        await client.CreateRepositoryAsync(
            new CreateRepositoryRequest
            {
                ImageScanningConfiguration = new ImageScanningConfiguration() { ScanOnPush = true },
                RepositoryName = repoName,
            }
        );
        try
        {
            var policyJson = GetPermissionJson(accountId);
            await client.SetRepositoryPolicyAsync(
                new SetRepositoryPolicyRequest { RepositoryName = repoName, PolicyText = policyJson, }
            );
        }
        catch (Exception e)
        {
            // To not have orphaned repos, delete the repo if setting the policy fails
            await client.DeleteRepositoryAsync(new DeleteRepositoryRequest { RepositoryName = repoName, });

            throw new Exception($"Unable to set ECR repo policy, deleting repo: {e}");
        }
    }
}
