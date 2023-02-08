using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityDetailDto
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public string? RootId { get; set; }
    public string? Description { get; set; }
    public MemberDto[] Members { get; set; } = Array.Empty<MemberDto>();
    public AwsAccountDto AwsAccount { get; set; }

    public static CapabilityDetailDto Create(Capability capability)
    {
        return new CapabilityDetailDto
        {
            Id = capability.Id,
            Name = capability.Name,
            RootId = capability.Id,
            Description = capability.Description,
            Members = capability.Memberships
                .Select(x => MemberDto.Create(x.Member))
                .ToArray(),
            AwsAccount = AwsAccountDto.Create(capability.AwsAccount)
        };
    }
}