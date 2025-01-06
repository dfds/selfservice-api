using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace SelfService.Application;

public class AwsRoleManager : IAwsRoleManger
{
    public async Task<AWSCredentials> AssumeRoleAsync(string roleArn, RegionEndpoint region)
    {
        using (var stsClient = new AmazonSecurityTokenServiceClient(region))
        {
            var assumeRoleRequest = new AssumeRoleRequest
            {
                RoleArn = roleArn,
                RoleSessionName = "SelfserviceECRRequests",
            };

            var assumeRoleResponse = await stsClient.AssumeRoleAsync(assumeRoleRequest);
            var credentials = assumeRoleResponse.Credentials;

            return new SessionAWSCredentials(
                credentials.AccessKeyId,
                credentials.SecretAccessKey,
                credentials.SessionToken
            );
        }
    }
}
