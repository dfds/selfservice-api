using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence.Queries;

public class MyCapabilitiesOutstandingActionsQuery : IMyCapabilitiesOutstandingActionsQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public MyCapabilitiesOutstandingActionsQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<CapabilityId, CapabilityOutstandingActions>> FindFor(
        IEnumerable<Capability> capabilities
    )
    {
        var capabilityList = capabilities.ToList();
        var capabilityIds = capabilityList.Select(c => c.Id).ToList();

        var pendingApplicationCounts = await _dbContext
            .MembershipApplications.Where(a =>
                capabilityIds.Contains(a.CapabilityId)
                && a.Status == MembershipApplicationStatusOptions.PendingApprovals
            )
            .GroupBy(a => a.CapabilityId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        return capabilityList.ToDictionary(
            c => c.Id,
            c => new CapabilityOutstandingActions
            {
                IsPendingDeletion = c.Status == CapabilityStatusOptions.PendingDeletion,
                PendingMembershipApplicationCount = pendingApplicationCounts.GetValueOrDefault(c.Id, 0),
            }
        );
    }
}
