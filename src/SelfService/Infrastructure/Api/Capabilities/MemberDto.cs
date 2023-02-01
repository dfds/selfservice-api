using SelfService.Legacy.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public record MemberDto(string Email)
{
    public static MemberDto Create(Membership member)
    {
        return new MemberDto(member.Email);
    }
}