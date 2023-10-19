using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Teams;

[Route("invitations")]
[Produces("application/json")]
[ApiController]
public class InvitationController : ControllerBase
{
    private readonly IInvitationApplicationService _invitationApplicationService;
    private readonly ApiResourceFactory _apiResourceFactory;

    public InvitationController(IInvitationApplicationService invitationApplicationService, ApiResourceFactory apiResourceFactory)
    {
        _invitationApplicationService = invitationApplicationService;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetActiveInvitationsForUser([FromRoute] string userId)
    {
        if (!User.TryGetUserId(out var principalId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        if (principalId != userId)
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        var invitations = await _invitationApplicationService.GetActiveInvitations(userId);

        return Ok(_apiResourceFactory.Convert(invitations, userId));
    }

    [HttpGet("user/{userId}/type/{targetType}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetActiveInvitationsForUserAndType([FromRoute] string userId, [FromRoute] string targetType)
    {
        if (!User.TryGetUserId(out var principalId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        if (principalId != userId)
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        if (!InvitationTargetTypeOptions.TryParse(targetType, out var targetTypeOption))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid target type", Detail = $"The target type {targetType} is not valid" }
            );
        }

        var invitations = await _invitationApplicationService.GetActiveInvitationsForType(userId, targetTypeOption);

        return Ok(_apiResourceFactory.Convert(invitations, userId, targetTypeOption.ToString()));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetInvitation([FromRoute] string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }
        
        if (!InvitationId.TryParse(id, out var invitationId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid invitation id", Detail = $"The invitation id {id} is not valid" }
            );
        }

        var invitation = await _invitationApplicationService.GetInvitation(invitationId);
        
        return Ok(_apiResourceFactory.Convert(invitation));
    }

    [HttpPost("{id}/accept")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> AcceptInvitation([FromRoute] string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }
        
        if (!InvitationId.TryParse(id, out var invitationId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid invitation id", Detail = $"The invitation id {id} is not valid" }
            );
        }

        var invitation = await _invitationApplicationService.AcceptInvitation(invitationId);
        
        return Ok(_apiResourceFactory.Convert(invitation));
    }

    [HttpPost("{id}/decline")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeclineInvitation([FromRoute] string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }
        
        if (!InvitationId.TryParse(id, out var invitationId))
        {
            return BadRequest(
                new ProblemDetails() { Title = "Invalid invitation id", Detail = $"The invitation id {id} is not valid" }
            );
        }

        var invitation = await _invitationApplicationService.DeclineInvitation(invitationId);

        return Ok(_apiResourceFactory.Convert(invitation));
    }
}
