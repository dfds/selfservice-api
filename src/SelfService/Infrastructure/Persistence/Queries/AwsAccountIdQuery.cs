using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence.Queries;

public class AwsAccountIdQuery : IAwsAccountIdQuery
{
    private readonly SelfServiceDbContext _context;

    public AwsAccountIdQuery(SelfServiceDbContext context)
    {
        _context = context;
    }

    public RealAwsAccountId? FindBy(CapabilityId capabilityId)
    {
        try
        {
            var query = _context.AwsAccounts.Where(x => x.CapabilityId == capabilityId).ToArray();
            return query.Length > 0 ? query.First().Registration.AccountId : null;
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyDictionary<CapabilityId, RealAwsAccountId> FindBy(IReadOnlyCollection<CapabilityId> capabilityIds)
    {
        try
        {
            var ids = capabilityIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<CapabilityId, RealAwsAccountId>();

            var accounts = _context.AwsAccounts.Where(x => ids.Contains(x.CapabilityId)).ToList();

            return accounts
                .Where(x => x.Registration.AccountId is not null)
                .GroupBy(x => x.CapabilityId)
                .ToDictionary(g => g.Key, g => g.First().Registration.AccountId!);
        }
        catch
        {
            return new Dictionary<CapabilityId, RealAwsAccountId>();
        }
    }
}
