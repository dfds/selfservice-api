using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace SelfService.Domain.Services;

public class AwsParameterStoreService
{
    private ILogger<AwsParameterStoreService> _logger;

    public AwsParameterStoreService(ILogger<AwsParameterStoreService> logger)
    {
        _logger = logger;
    }

    private AmazonSimpleSystemsManagementClient NewClient()
    {
        return new AmazonSimpleSystemsManagementClient(RegionEndpoint.EUWest1);
    }

    public async Task PutParameter(PutParameterRequest request)
    {
        var client = NewClient();
        await client.PutParameterAsync(request);
    }

    public async Task<bool> HasParameter(GetParameterRequest request)
    {
        var client = NewClient();
        try
        {
            await client.GetParameterAsync(request);
        }
        catch (ParameterNotFoundException)
        {
            return false;
        }

        return true;
    }

    public async Task DeleteParameter(DeleteParameterRequest request)
    {
        var client = NewClient();
        await client.DeleteParameterAsync(request);
    }
}
