using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsAccountApplicationService
{
    Task<AwsAccountId> RequestAwsAccount(CapabilityId capabilityId, UserId requestedBy);
}