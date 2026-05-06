using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IMyCapabilitiesOutstandingActionsQuery
{
    Task<Dictionary<CapabilityId, CapabilityOutstandingActions>> FindFor(IEnumerable<Capability> capabilities);
}
