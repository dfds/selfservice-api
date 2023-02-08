using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public record MemberDto(string UPN)
{
    public static MemberDto Create(Member member)
    {
        return new MemberDto(member.UPN);
    }
}