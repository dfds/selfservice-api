using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsAccountApplicationService
{
    Task<AwsAccountId> RequestAwsAccount(CapabilityId capabilityId, UserId requestedBy);
    Task CreateAwsAccountRequestTicket(AwsAccountId id);
    Task RegisterRealAwsAccount(AwsAccountId id, RealAwsAccountId realAwsAccountId, string? roleEmail);
}