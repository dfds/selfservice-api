using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Teams;

public class AddTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> LinkedCapabilityIds { get; set; } = new();
}
