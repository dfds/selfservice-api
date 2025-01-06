using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Teams;

[Route("teams")]
[Produces("application/json")]
[ApiController]
public class TeamController : ControllerBase
{
    private readonly ITeamApplicationService _teamApplicationService;
    private readonly ApiResourceFactory _apiResourceFactory;

    public TeamController(ITeamApplicationService teamApplicationService, ApiResourceFactory apiResourceFactory)
    {
        _teamApplicationService = teamApplicationService;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetAllTeams()
    {
        var teams = await _teamApplicationService.GetAllTeams();

        return Ok(_apiResourceFactory.Convert(teams));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTeam([FromRoute] string id)
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

        return Ok(_apiResourceFactory.Convert(team));
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> AddTeam([FromBody] AddTeamRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        List<CapabilityId> linkedCapabilityIds = new List<CapabilityId>();

        foreach (var requestLinkedCapabilityId in request.LinkedCapabilityIds)
        {
            if (!CapabilityId.TryParse(requestLinkedCapabilityId, out var capabilityId))
            {
                return BadRequest(
                    new ProblemDetails
                    {
                        Title = "Invalid capability id",
                        Detail = $"The capability id {requestLinkedCapabilityId} is not valid",
                    }
                );
            }

            linkedCapabilityIds.Add(capabilityId);
        }

        var team = await _teamApplicationService.AddTeam(
            request.Name,
            request.Description,
            userId,
            linkedCapabilityIds
        );

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> RemoveTeam([FromRoute] string id)
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

    [HttpPost("{id}/capability-links/{capabilityId}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> AddLinkToCapability([FromRoute] string id, [FromRoute] string capabilityId)
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
                    Detail = $"The capability id {capabilityId} is not valid",
                }
            );
        }

        if (UserId.TryParse(User.Identity?.Name, out var userId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        await _teamApplicationService.AddLinkToCapability(teamId, capabilityIdParsed, userId);

        return NoContent();
    }

    [HttpDelete("{id}/capability-links/{capabilityId}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> RemoveLinkToCapability([FromRoute] string id, [FromRoute] string capabilityId)
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
                    Detail = $"The capability id {capabilityId} is not valid",
                }
            );
        }

        await _teamApplicationService.RemoveLinkToCapability(teamId, capabilityIdParsed);

        return NoContent();
    }

    [HttpGet("{id}/capability-links")]
    public async Task<IActionResult> GetLinkedCapabilities([FromRoute] string id)
    {
        if (!TeamId.TryParse(id, out var teamId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid team id", Detail = $"The team id {id} is not valid" }
            );
        }

        var capabilities = await _teamApplicationService.GetLinkedCapabilities(teamId);

        return Ok(await _apiResourceFactory.Convert(capabilities));
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers([FromRoute] string id)
    {
        if (!TeamId.TryParse(id, out var teamId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid team id", Detail = $"The team id {id} is not valid" }
            );
        }

        var users = await _teamApplicationService.GetMembers(teamId);

        return Ok(users);
    }
}
