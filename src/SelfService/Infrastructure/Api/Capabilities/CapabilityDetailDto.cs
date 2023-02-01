using SelfService.Legacy.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityDetailDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? RootId { get; set; }
    public string? Description { get; set; }
    public MemberDto[] Members { get; set; } = Array.Empty<MemberDto>();
    public ContextDto[] Contexts { get; set; } = Array.Empty<ContextDto>();

    public static CapabilityDetailDto Create(Capability capability)
    {
        return new CapabilityDetailDto
        {
            Id = capability.Id,
            Name = capability.Name,
            RootId = capability.RootId,
            Description = capability.Description,
            Members = capability.Memberships
                .Select(MemberDto.Create)
                .ToArray(),
            Contexts = capability
                .Contexts
                .Select(ContextDto.Create)
                .ToArray(),
        };
    }
}