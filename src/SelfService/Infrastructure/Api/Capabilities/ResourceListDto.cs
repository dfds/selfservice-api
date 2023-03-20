namespace SelfService.Infrastructure.Api.Capabilities;

public class ResourceListDto<TDto> : ResourceDtoBase
{
    public TDto[] Items { get; set; }
}