namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityListDto : ResourceDtoBase
{
    public CapabilityApiResource[] Items { get; set; }
}