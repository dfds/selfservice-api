using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface ICapabilityDeletionStatusQuery
{
    Task<Boolean> isPendingDeletion(CapabilityId Id);
}