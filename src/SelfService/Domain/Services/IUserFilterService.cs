using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IUserFilterService
{
    Task<UserAudienceResolution> ResolveUsers(string audienceJson);
}

public class UserAudienceResolution
{
    public List<Member> Members { get; init; } = new();

    /// <summary>
    /// For "specific" mode: emails that did not match any existing member.
    /// Empty for other modes.
    /// </summary>
    public List<string> UnmatchedEmails { get; init; } = new();
}
