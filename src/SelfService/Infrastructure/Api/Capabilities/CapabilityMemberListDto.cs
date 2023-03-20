namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityMemberListDto : ResourceDtoBase
{
    public MemberDto[] Items { get; set; }
}