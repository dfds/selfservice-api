using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Persistence;
using Capability = SelfService.Domain.Models.Capability;

namespace SelfService.Infrastructure.Api.MembershipApplications;

[Route("membershipapplications")]
[Produces("application/json")]
[ApiController]
public class MembershipApplicationController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IMembershipApplicationService _membershipApplicationService;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;
    private readonly ApiResourceFactory _apiResourceFactory;

    public MembershipApplicationController(ApiResourceFactory apiResourceFactory, IAuthorizationService authorizationService, 
        IMembershipApplicationService membershipApplicationService, IMembershipApplicationRepository membershipApplicationRepository)
    {
        _apiResourceFactory = apiResourceFactory;
        _authorizationService = authorizationService;
        _membershipApplicationService = membershipApplicationService;
        _membershipApplicationRepository = membershipApplicationRepository;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MembershipApplicationApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetById(string id, [FromServices] SelfServiceDbContext dbContext)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!MembershipApplicationId.TryParse(id, out var membershipApplicationId))
        {
            return NotFound();
        }

        var application = await dbContext.MembershipApplications.SingleOrDefaultAsync(x => x.Id == membershipApplicationId);
        if (application is null)
        {
            return NotFound();
        }

        var accessLevelForCapability = await _authorizationService.GetUserAccessLevelForCapability(userId, application.CapabilityId);
        if (accessLevelForCapability == UserAccessLevelOptions.Read && application.Applicant != userId)
        {
            // user is not member of capability and the membership application does not belong to THIS user
            return Unauthorized();
        }

        return Ok(_apiResourceFactory.Convert(application, accessLevelForCapability, userId));
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(MembershipApplicationApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> SubmitMembershipApplication([FromBody] NewMembershipApplicationRequest newMembershipApplication)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!CapabilityId.TryParse(newMembershipApplication.CapabilityId, out var capabilityId))
        {
            ModelState.AddModelError(nameof(newMembershipApplication.CapabilityId), "Invalid capability id.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        try
        {
            var applicationId = await _membershipApplicationService.SubmitMembershipApplication(capabilityId, userId);
            var membershipApplication = await _membershipApplicationRepository.Get(applicationId);

            return CreatedAtRoute(
                routeName: nameof(GetById),
                routeValues: new {id = applicationId.ToString()},
                value: _apiResourceFactory.Convert(membershipApplication, UserAccessLevelOptions.ReadWrite, userId)
            );
        }
        catch (EntityNotFoundException<Capability>)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Capability not found",
                Detail = $"Capability \"{capabilityId}\" is unknown by the system."
            });
        }
        catch (PendingMembershipApplicationAlreadyExistsException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Already has pending membership application",
                Detail = $"User \"{userId}\" already has a pending membership application for capability \"{capabilityId}\"."
            });
        }
    }

    [HttpGet("{id}/approvals")]
    [ProducesResponseType(typeof(MembershipApprovalListApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetMembershipApplicationApprovals(string id, [FromServices] SelfServiceDbContext dbContext)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Access denied!",
                Detail = $"User is not authorized to access membership application \"{id}\"."
            });
        }

        if (!MembershipApplicationId.TryParse(id, out var membershipApplicationId))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Membership application not found",
                Detail = $"Membership application \"{membershipApplicationId}\" is unknown by the system."
            });
        }

        try
        {
            var application = await _membershipApplicationRepository.Get(membershipApplicationId);

            var accessLevelForCapability = await _authorizationService.GetUserAccessLevelForCapability(userId, application.CapabilityId);
            if (accessLevelForCapability == UserAccessLevelOptions.Read && application.Applicant != userId)
            {
                // user is not member of capability and the membership application does not belong to the user
                return Unauthorized(new ProblemDetails
                {
                    Title = "Access denied!",
                    Detail = $"User \"{userId}\" is not authorized to access membership application \"{membershipApplicationId}\"."
                });
            }

            var parent = _apiResourceFactory.Convert(application,accessLevelForCapability, userId);
            return Ok(parent.Approvals);
        }
        catch (EntityNotFoundException<MembershipApplication>)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Membership application not found",
                Detail = $"Membership application \"{membershipApplicationId}\" is unknown by the system."
            });
        }
    }

    [HttpPost("{id}/approvals")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> SubmitApproval(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unknown user id",
                Detail = $"User id is not valid and cannot be used to approve a membership application.",
            });
        }

        if (!MembershipApplicationId.TryParse(id, out var membershipApplicationId))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Membership application not found.",
                Detail = $"A membership application with id \"{id}\" could not be found."
            });
        }

        try
        {
            await _membershipApplicationService.ApproveMembershipApplication(membershipApplicationId, userId);
            return NoContent();
        }
        catch (EntityNotFoundException<MembershipApplication>)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Membership application not found.",
                Detail = $"A membership application with id \"{id}\" could not be found."
            });
        }
        catch (NotAuthorizedToApproveMembershipApplication)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Not authorized to approve",
                Detail = $"User \"{userId}\" is not authorized to approve membership application \"{membershipApplicationId}\".", 
            });
        }
    }
}