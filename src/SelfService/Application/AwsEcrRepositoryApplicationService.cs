using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;

namespace SelfService.Application;

public class AwsEcrRepositoryApplicationService : IAwsECRRepositoryApplicationService
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

    private AmazonECRClient NewAwsECRClient()
    {
        return new AmazonECRClient(new AmazonECRConfig { RegionEndpoint = RegionEndpoint.EUCentral1, });
    }

    /// <summary>
    /// 1. We create the repo
    /// 2. We set the policy to allow the account to pull from the repo
    ///
    /// In case of failure setting the policy we delete the repo to not have orphaned repos
    /// </summary>
    public async Task CreateECRRepo(string name)
    {
        var client = NewAwsECRClient();
        var accountId = Environment.GetEnvironmentVariable("ECR_PULL_PERMISSION_AWS_ACCOUNT_ID");
        if (accountId == null)
            throw new Exception("ECR_PULL_PERMISSION_AWS_ACCOUNT_ID environment variable is not set");

        await client.CreateRepositoryAsync(
            new CreateRepositoryRequest
            {
                ImageScanningConfiguration = new ImageScanningConfiguration() { ScanOnPush = true },
                RepositoryName = name,
            }
        );
        try
        {
            var policyJson = GetPermissionJson(accountId);
            await client.SetRepositoryPolicyAsync(
                new SetRepositoryPolicyRequest { RepositoryName = name, PolicyText = policyJson, }
            );
        }
        catch (Exception e)
        {
            // To not have orphaned repos, delete the repo if setting the policy fails
            await client.DeleteRepositoryAsync(new DeleteRepositoryRequest { RepositoryName = name, });

            throw new Exception($"Unable to set ECR repo policy, deleting repo: {e}");
        }
    }

    /// <summary>
    /// Only for system use, and currently used if adding ECR repository to db fails
    /// </summary>
    public async Task DeleteECRRepo(string name)
    {
        AmazonECRClient client = new(new AmazonECRConfig { RegionEndpoint = RegionEndpoint.EUCentral1, });
        await client.DeleteRepositoryAsync(new DeleteRepositoryRequest { RepositoryName = name, });
    }

    public async Task<List<string>> GetECRRepositories()
    {
        var client = NewAwsECRClient();
        HashSet<string> repos = new();
        string? nextToken = null;
        do
        {
            var response = await client.DescribeRepositoriesAsync(
                new DescribeRepositoriesRequest() { NextToken = nextToken }
            );
            response.Repositories.ForEach(x => repos.Add(x.RepositoryName));
            nextToken = response.NextToken;
        } while (nextToken != null);

        return repos.ToList();
    }
}
