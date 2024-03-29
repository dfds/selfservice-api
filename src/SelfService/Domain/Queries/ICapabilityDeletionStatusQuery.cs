using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface ICapabilityDeletionStatusQuery
{
    Task<bool> IsPendingDeletion(CapabilityId id);
}
