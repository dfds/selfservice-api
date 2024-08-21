using SelfService.Domain.Models;

namespace SelfService.Application;

public class AwsMockApplicationService : IAwsEC2QueriesApplicationService, IAwsECRRepositoryApplicationService
{
    private List<string> ecrRepositories =
        new(new string[] { "predetermined repository 1", "predetermined repository 2", "predetermined repository 3" });
    private List<VPCInformation> vpcs = new();

    /*
    new(
        new VPCInformation[]
        {
            new VPCInformation
            {
                VpcId = "vpc-123456",
                CidrBlock = "10.225.0.64/26",
                Region = "eu-central-1"
            }
        }
    );
    */

    public async Task CreateECRRepo(string name)
    {
        ecrRepositories.Add(name);
        await Task.CompletedTask;
    }

    public async Task DeleteECRRepo(string name)
    {
        ecrRepositories.Remove(name);
        await Task.CompletedTask;
    }

    public async Task<List<string>> GetECRRepositories()
    {
        return await Task.FromResult(ecrRepositories);
    }

    public async Task<List<VPCInformation>> GetVPCsAsync(string accountId)
    {
        return await Task.FromResult(vpcs);
    }
}
