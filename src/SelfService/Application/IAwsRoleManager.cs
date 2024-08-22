using Amazon;
using Amazon.Runtime;

namespace SelfService.Application;

public interface IAwsRoleManger
{
    Task<AWSCredentials> AssumeRoleAsync(string roleArn, RegionEndpoint region);
}
