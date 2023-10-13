using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Teams;

[Route("teams")]
[Produces("application/json")]
[ApiController]
public class TeamsController : ControllerBase
{
    private readonly ITeamApplicationService _teamApplicationService;

    public TeamsController(ITeamApplicationService teamApplicationService)
    {
        _teamApplicationService = teamApplicationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetAllTeams()
    {
        var teams = await _teamApplicationService.GetAllTeams();

        return Ok(teams);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeam([FromRoute] Guid id)
    {
        if (!TeamId.TryParse(id, out var teamId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid team id", Detail = $"The team id {id} is not valid" }
            );
        }

        var team = await _teamApplicationService.GetTeam(teamId);

        if (team == null)
        {
            return NotFound();
        }

        return Ok(team);
    }

    [HttpPost("")]
    public async Task<IActionResult> AddTeam([FromBody] AddTeamRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        var team = await _teamApplicationService.AddTeam(request.Name, request.Description, userId);

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveTeam([FromRoute] Guid id)
    {
        if (!TeamId.TryParse(id, out var teamId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid team id", Detail = $"The team id {id} is not valid" }
            );
        }

        await _teamApplicationService.RemoveTeam(teamId);

        return NoContent();
    }

    [HttpPost("{id}/associate-capability/{capabilityId}")]
    public async Task<IActionResult> AddAssociationWithCapability([FromRoute] Guid id, [FromRoute] string capabilityId)
    {
        if (!TeamId.TryParse(id, out var teamId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid team id", Detail = $"The team id {id} is not valid" }
            );
        }

        if (!CapabilityId.TryParse(capabilityId, out var capabilityIdParsed))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid capability id",
                    Detail = $"The capability id {capabilityId} is not valid"
                }
            );
        }

        await _teamApplicationService.AddAssociationWithCapability(teamId, capabilityIdParsed);

        return NoContent();
    }

    [HttpDelete("{id}/associate-capability/{capabilityId}")]
    public async Task<IActionResult> RemoveAssociationWithCapability(
        [FromRoute] Guid id,
        [FromRoute] string capabilityId
    )
    {
        if (!TeamId.TryParse(id, out var teamId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid team id", Detail = $"The team id {id} is not valid" }
            );
        }

        if (!CapabilityId.TryParse(capabilityId, out var capabilityIdParsed))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid capability id",
                    Detail = $"The capability id {capabilityId} is not valid"
                }
            );
        }

        await _teamApplicationService.RemoveAssociationWithCapability(teamId, capabilityIdParsed);

        return NoContent();
    }
}
