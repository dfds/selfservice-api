using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IAwsAccountIdQuery
{
    RealAwsAccountId? FindBy(CapabilityId capabilityId);

    /// <summary>
    /// Bulk variant of <see cref="FindBy(CapabilityId)"/>: resolves the AWS account id for many
    /// capabilities in a single query. Capabilities without a registered account are absent from the result.
    /// </summary>
    IReadOnlyDictionary<CapabilityId, RealAwsAccountId> FindBy(IReadOnlyCollection<CapabilityId> capabilityIds);
}
