using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public record CapabilityListItemDto(Guid Id, string? Name, string? RootId, string? Description)
{
    public static CapabilityListItemDto Create(Capability capability)
    {
        return new CapabilityListItemDto(
            capability.Id,
            capability.Name,
            capability.RootId,
            capability.Description
        );
    }
}