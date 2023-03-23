namespace SelfService.Infrastructure.Api.Capabilities;

[Obsolete]
public class ResourceListDto<TDto> : ResourceDtoBase
{
    public TDto[] Items { get; set; }
}