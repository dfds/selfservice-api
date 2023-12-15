using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IAwsAccountIdQuery
{
    RealAwsAccountId? FindBy(CapabilityId capabilityId);
}
