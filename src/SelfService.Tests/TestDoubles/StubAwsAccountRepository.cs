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
        return Task.FromResult<AwsAccount?>(_awsAccount);
    }

    public Task<AwsAccount?> FindBy(CapabilityId capabilityId, string environment)
    {
        if (_awsAccount?.Environment == environment)
            return Task.FromResult<AwsAccount?>(_awsAccount);
        return Task.FromResult<AwsAccount?>(null);
    }

    public Task<List<AwsAccount>> GetAllBy(CapabilityId capabilityId)
    {
        if (_awsAccount?.CapabilityId == capabilityId)
            return Task.FromResult(new List<AwsAccount> { _awsAccount });
        return Task.FromResult(new List<AwsAccount>());
    }

    public Task<List<AwsAccount>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<List<AwsAccount>> GetByCapabilityIds(IEnumerable<CapabilityId> capabilityIds)
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

    public Task<bool> Exists(CapabilityId capabilityId, string environment)
    {
        if (_awsAccount == null)
            return Task.FromResult(false);
        return Task.FromResult(_awsAccount.Environment == environment);
    }

    public Task<AwsAccount?> FindBy(AwsAccountId id)
    {
        if (_awsAccount?.Id == id)
            return Task.FromResult<AwsAccount?>(_awsAccount);
        return Task.FromResult<AwsAccount?>(null);
    }

    public Task<int> CountBy(CapabilityId capabilityId)
    {
        return Task.FromResult(_awsAccount?.CapabilityId == capabilityId ? 1 : 0);
    }
}
