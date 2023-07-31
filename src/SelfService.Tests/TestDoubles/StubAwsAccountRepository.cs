using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubAwsAccountRepository : IAwsAccountRepository
{
    private readonly AwsAccount? _awsAccount;

    public StubAwsAccountRepository(AwsAccount? awsAccount = null)
    {
        _awsAccount = awsAccount;
    }

    public Task<AwsAccount?> FindBy(CapabilityId capabilityId)
    {
        return Task.FromResult(_awsAccount);
    }

    public Task<List<AwsAccount>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<AwsAccount> Get(AwsAccountId id)
    {
        throw new NotImplementedException();
    }

    public Task Add(AwsAccount account)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Exists(CapabilityId capabilityId)
    {
        return Task.FromResult(_awsAccount != null);
    }
}
