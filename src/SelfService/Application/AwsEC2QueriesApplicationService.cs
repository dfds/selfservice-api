using Amazon;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.Runtime;
using Amazon.EC2;
using Amazon.EC2.Model;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class AwsEC2QueriesApplicationService : IAwsEC2QueriesApplicationService
{
    private IAwsRoleManger awsRoleManager => new AwsRoleManager();

    public async Task<List<VPCInformation>> GetVPCsAsync(string accountId)
    {
        var allVpcs = new List<VPCInformation>();

        string RoleArn = $"arn:aws:iam::{accountId}:role/ReadVPCPeerings";

        var regions = new List<RegionEndpoint>
        {
            RegionEndpoint.EUCentral1,
            RegionEndpoint.EUWest1,
            RegionEndpoint.EUWest2,
            RegionEndpoint.USEast1,
            RegionEndpoint.USWest1,
        };

        var temporaryCredentials = await awsRoleManager.AssumeRoleAsync(RoleArn, RegionEndpoint.EUCentral1);

        foreach (var region in regions)
        {
            var ec2Client = new AmazonEC2Client(temporaryCredentials, region);
            var vpcs = await DescribeVpcsAsync(ec2Client);

            if (vpcs != null)
            {
                foreach (var vpc in vpcs)
                {
                    var VpcInformation = new VPCInformation
                    {
                        VpcId = vpc.VpcId,
                        CidrBlock = vpc.CidrBlock,
                        Region = region.SystemName
                    };
                    allVpcs.Add(VpcInformation);
                }
            }
        }
        return allVpcs;
    }

    static async Task<List<Vpc>> DescribeVpcsAsync(IAmazonEC2 ec2Client)
    {
        var result = new List<Vpc>();

        try
        {
            var vpcresponse = await ec2Client.DescribeVpcsAsync();
            if (vpcresponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                foreach (var vpc in vpcresponse.Vpcs)
                {
                    var describeTagsRequest = new DescribeTagsRequest
                    {
                        Filters = new List<Filter>
                        {
                            new Filter
                            {
                                Name = "resource-id",
                                Values = new List<string> { vpc.VpcId }
                            }
                        }
                    };
                    var tagsResponse = await ec2Client.DescribeTagsAsync(describeTagsRequest);

                    var hasPeeringTag = tagsResponse.Tags.Exists(tag => tag.Key == "Name" && tag.Value == "peering");
                    if (hasPeeringTag)
                    {
                        result.Add(vpc);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error in region {ec2Client.Config.RegionEndpoint.SystemName}: {ex.Message}");
        }
        return result;
    }
}
