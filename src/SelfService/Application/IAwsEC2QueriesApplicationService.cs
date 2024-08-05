using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAwsEC2QueriesApplicationService
{
    Task<List<VPCInformation>> GetVPCsAsync(string accountId);
}
