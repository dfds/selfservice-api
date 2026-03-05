using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.MembershipApplications;

[Route("membershipapplications")]
[Produces("application/json")]
[ApiController]
public class MembershipApplicationController : ControllerBase
{
    private readonly ILogger<MembershipApplicationController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMembershipApplicationService _membershipApplicationService;
    private readonly IMembershipApplicationQuery _membershipApplicationQuery;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly RbacApplicationService _rbacApplicationService;

    public MembershipApplicationController(
        ILogger<MembershipApplicationController> logger,
        IAuthorizationService authorizationService,
        IMembershipApplicationService membershipApplicationService,
        IMembershipApplicationQuery membershipApplicationQuery,
        ApiResourceFactory apiResourceFactory,
        RbacApplicationService rbacApplicationService
    )
    {
        _authorizationService = authorizationService;
        _membershipApplicationService = membershipApplicationService;
        _membershipApplicationQuery = membershipApplicationQuery;
        _logger = logger;
        _apiResourceFactory = apiResourceFactory;
        _rbacApplicationService = rbacApplicationService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MembershipApplicationApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!MembershipApplicationId.TryParse(id, out var membershipApplicationId))
        {
            return NotFound();
        }

        var application = await _membershipApplicationQuery.FindById(membershipApplicationId);
        if (application is null)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "MembershipApplication not found",
                    Detail = $"MembershipApplication \"{membershipApplicationId}\" is unknown by the system.",
                }
            );
        }

        if (!await _authorizationService.CanReadMembershipApplications(userId, application))
        {
            // user is not member of capability and the membership application does not belong to THIS user
            return Unauthorized();
        }

        return Ok(_apiResourceFactory.Convert(application, userId));
    }

    [HttpGet("{id}/approvals")]
    [ProducesResponseType(typeof(MembershipApprovalListApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetMembershipApplicationApprovals(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access denied!",
                    Detail = $"User is not authorized to access membership application \"{id}\".",
                }
            );
        }

        if (!MembershipApplicationId.TryParse(id, out var membershipApplicationId))
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Membership application not found",
                    Detail = $"Membership application \"{membershipApplicationId}\" is unknown by the system.",
                }
            );
        }

        var application = await _membershipApplicationQuery.FindById(membershipApplicationId);
        if (application == null)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Membership application not found",
                    Detail = $"Membership application \"{membershipApplicationId}\" is unknown by the system.",
                }
            );
        }

        if (!await _authorizationService.CanReadMembershipApplications(userId, application))
        {
            // user is not member of capability and the membership application does not belong to THIS user
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access denied!",
                    Detail =
                        $"User \"{userId}\" is not authorized to access membership application \"{membershipApplicationId}\".",
                }
            );
        }

        var parent = _apiResourceFactory.Convert(application, userId);
        return Ok(parent.Approvals);
    }

    [HttpDelete("{id}/approvals")]
    [ProducesResponseType(typeof(MembershipApplicationApiResource), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteCapabilityMembershipApplications(string id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = $"User id is not valid and cannot be used to delete a membership application.",
                }
            );
        }

        if (!MembershipApplicationId.TryParse(id, out var membershipApplicationId))
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Membership application not found.",
                    Detail = $"A membership application with id \"{id}\" could not be found.",
                }
            );
        }

        var portalUser = HttpContext.User.ToPortalUser();
        if (!await _authorizationService.CanDeleteMembershipApplication(portalUser, userId, membershipApplicationId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "User unauthorized",
                    Detail = $"user \"{userId}\" isn't authorized to delete membership applications.",
                }
            );
        }

        await _membershipApplicationService.RemoveMembershipApplication(membershipApplicationId);
        return NoContent();
    }

    [HttpPost("{id}/approvals")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> SubmitApproval(string id, [FromBody] RBAC.Dto.RbacRoleGrant? request = null)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = $"User id is not valid and cannot be used to approve a membership application.",
                }
            );
        }

        if (!MembershipApplicationId.TryParse(id, out var membershipApplicationId))
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Membership application not found.",
                    Detail = $"A membership application with id \"{id}\" could not be found.",
                }
            );
        }

        var application = await _membershipApplicationQuery.FindById(membershipApplicationId);
        if (application == null)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Membership application not found.",
                    Detail = $"A membership application with id \"{id}\" could not be found.",
                }
            );
        }

        try
        {
            // If request is null, create a legal RbacRoleGrant for Reader role
            if (request == null)
            {
                // Fetch the Reader role from assignable roles
                var roles = await _rbacApplicationService.GetAssignableRoles();
                var readerRole = roles.FirstOrDefault(r => r.Name == "Reader");
                if (readerRole == null)
                {
                    return BadRequest(
                        new ProblemDetails
                        {
                            Title = "No Reader role available",
                            Detail = "Could not find a Reader role to assign.",
                        }
                    );
                }

                var readerRoleId = _rbacApplicationService
                    .GetAssignableRoles()
                    .Result.Where(r => r.Name == "Reader")
                    .Select(r => r.Id)
                    .ToList()
                    .First();
                Domain.Models.RbacRoleGrant readerRoleGrant = Domain.Models.RbacRoleGrant.New(
                    readerRoleId!,
                    AssignedEntityType.User,
                    application.Applicant,
                    RbacAccessType.Capability,
                    application.CapabilityId.ToString()
                );
                await _rbacApplicationService.GrantRoleGrant(userId.ToString(), readerRoleGrant);
            }
            else
            {
                await _rbacApplicationService.GrantRoleGrant(userId.ToString(), request.IntoDomainModel());
            }
            await _membershipApplicationService.ApproveMembershipApplication(membershipApplicationId, userId);
        }
        catch (EntityNotFoundException<MembershipApplication>)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Membership application not found.",
                    Detail = $"A membership application with id \"{id}\" could not be found.",
                }
            );
        }
        catch (NotAuthorizedToApproveMembershipApplication)
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Not authorized to approve",
                    Detail =
                        $"User \"{userId}\" is not authorized to approve membership application \"{membershipApplicationId}\".",
                }
            );
        }
        return NoContent();
    }

    [HttpGet("eligible-for-approval")]
    [ProducesResponseType(typeof(MembershipApplicationListApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> MembershipsThatUserCanApprove()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = $"User id is not valid and cannot be used to approve a membership application.",
                }
            );
        }
        var result = await _membershipApplicationService.GetMembershipsApplicationsThatUserCanApprove(userId);

        return Ok(await _apiResourceFactory.Convert(result, userId));
    }

    [HttpGet("my-outstanding-applications")]
    [ProducesResponseType(typeof(MembershipApplicationListApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> MyOutstandingApplications()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unknown user id",
                    Detail = $"User id is not valid and cannot be used to approve a membership application.",
                }
            );
        }
        var result = await _membershipApplicationService.GetOutstandingMembershipsApplicationsForUser(userId);

        return Ok(await _apiResourceFactory.ConvertOutstandingApplications(result, userId));
    }
}
