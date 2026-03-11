using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IUserEmailQuery
{
    Task<IEnumerable<UserEmailInfo>> GetUsersWithFilters(
        IEnumerable<string>? roles,
        IEnumerable<string>? costCentres,
        IEnumerable<string>? businessCapabilities,
        IEnumerable<string>? capabilities
    );
}

public class UserEmailInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
