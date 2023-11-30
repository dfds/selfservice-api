using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Tests.TestDoubles;

public class StubCapabilityDeletionStatusQuery : ICapabilityDeletionStatusQuery
{
    private readonly bool _isPendingDeletion;

    public StubCapabilityDeletionStatusQuery(bool isPendingDeletion = false)
    {
        _isPendingDeletion = isPendingDeletion;
    }

    public Task<bool> IsPendingDeletion(CapabilityId id)
    {
        return Task.FromResult(_isPendingDeletion);
    }
}
