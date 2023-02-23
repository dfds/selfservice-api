using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IMyCapabilitiesQuery
{
    Task<IEnumerable<Capability>> FindBy(UserId userId);
}