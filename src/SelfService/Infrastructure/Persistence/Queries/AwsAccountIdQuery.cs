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
}
