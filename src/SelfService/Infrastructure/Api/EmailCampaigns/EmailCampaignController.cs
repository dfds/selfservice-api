using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.EmailCampaigns;

[Route("email-campaigns")]
[Produces("application/json")]
[ApiController]
public class EmailCampaignController : ControllerBase
{
    private readonly IEmailCampaignApplicationService _emailCampaignApplicationService;

    public EmailCampaignController(IEmailCampaignApplicationService emailCampaignApplicationService)
    {
        _emailCampaignApplicationService = emailCampaignApplicationService;
    }

    private const string DefaultAudienceJson = "{\"mode\":\"all\"}";

    private bool IsAuthorized()
    {
        var portalUser = HttpContext.User.ToPortalUser();
        return portalUser.Roles.Any(role => role == UserRole.CloudEngineer);
    }

    private IActionResult? ParseCampaignId(string id, out EmailCampaignId parsedId) =>
        EmailCampaignId.TryParse(id, out parsedId)
            ? null
            : BadRequest(new ProblemDetails { Title = "Invalid Id", Detail = $"{id} is not a valid email campaign id." });

    private IActionResult? ValidateCampaignRequest(CreateEmailCampaignRequest request) =>
        string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Subject) || string.IsNullOrEmpty(request.ContentJson)
            ? BadRequest(new ProblemDetails { Title = "Invalid Request", Detail = "Name, subject, and content are required." })
            : null;

    [HttpGet("variables")]
    public async Task<IActionResult> GetTemplateVariables()
    {
        if (!IsAuthorized())
            return Unauthorized();

        try
        {
            var variables = await _emailCampaignApplicationService.GetTemplateVariables();
            return Ok(
                variables.Select(v => new TemplateVariableApiResource
                {
                    Name = v.Name,
                    Description = v.Description,
                    Entity = v.Entity,
                    Example = v.Example,
                })
            );
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetTemplateVariables: {e.Message}." }
            );
        }
    }

    [HttpGet("")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        if (!IsAuthorized())
            return Unauthorized();

        try
        {
            var campaigns = await _emailCampaignApplicationService.GetAll(status);
            return Ok(campaigns.Select(ToApiResource));
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetEmailCampaigns: {e.Message}." }
            );
        }
    }

    [HttpGet("{id:required}")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            var campaign = await _emailCampaignApplicationService.GetById(parsedId);
            if (campaign == null)
            {
                return NotFound(
                    new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"Email campaign with id {id} does not exist.",
                    }
                );
            }
            return Ok(ToApiResource(campaign));
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] CreateEmailCampaignRequest request)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (ValidateCampaignRequest(request) is { } validationError) return validationError;

        try
        {
            EmailCampaignScheduleType? parsedScheduleType = null;
            if (!string.IsNullOrEmpty(request.ScheduleType) &&
                EmailCampaignScheduleType.TryParse(request.ScheduleType, out var st))
            {
                parsedScheduleType = st;
            }

            var campaign = await _emailCampaignApplicationService.CreateDraft(
                request.Name!,
                request.Subject!,
                request.ContentJson!,
                request.ContentHtml,
                request.AudienceJson ?? DefaultAudienceJson,
                request.RecipientFilter,
                userId.ToString(),
                parsedScheduleType,
                request.ScheduledAt,
                request.CronExpression
            );
            return CreatedAtAction(nameof(GetById), new { id = campaign.Id.ToString() }, ToApiResource(campaign));
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"CreateEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpPut("{id:required}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateEmailCampaignRequest request)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        if (ValidateCampaignRequest(request) is { } validationError) return validationError;

        try
        {
            EmailCampaignScheduleType? parsedScheduleType = null;
            if (!string.IsNullOrEmpty(request.ScheduleType) &&
                EmailCampaignScheduleType.TryParse(request.ScheduleType, out var st))
            {
                parsedScheduleType = st;
            }

            await _emailCampaignApplicationService.UpdateDraft(
                parsedId,
                request.Name!,
                request.Subject!,
                request.ContentJson!,
                request.ContentHtml,
                request.AudienceJson ?? DefaultAudienceJson,
                request.RecipientFilter,
                userId.ToString(),
                parsedScheduleType,
                request.ScheduledAt,
                request.CronExpression
            );
            return NoContent();
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"UpdateEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpDelete("{id:required}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            await _emailCampaignApplicationService.DeleteDraft(parsedId);
            return NoContent();
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"DeleteEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}/duplicate")]
    public async Task<IActionResult> Duplicate(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            var duplicate = await _emailCampaignApplicationService.DuplicateCampaign(parsedId, userId.ToString());
            return Ok(ToApiResource(duplicate));
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"DuplicateEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpPost("resolve-audience")]
    public async Task<IActionResult> ResolveAudience([FromBody] ResolveAudienceRequest request)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (string.IsNullOrEmpty(request.AudienceJson))
        {
            return BadRequest(
                new ProblemDetails { Title = "Invalid Request", Detail = "AudienceJson is required." }
            );
        }

        try
        {
            var result = await _emailCampaignApplicationService.ResolveAudience(request.AudienceJson, request.RecipientFilter);
            return Ok(new ResolveAudienceResponse
            {
                TotalCapabilities = result.TotalCapabilities,
                TotalRecipients = result.TotalRecipients,
                Capabilities = result.Capabilities.Select(c => new AudienceCapabilityItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    MemberCount = c.MemberCount,
                    Recipients = c.Recipients.Select(r => new RecipientItem
                    {
                        Email = r.Email,
                        DisplayName = r.DisplayName,
                    }).ToList(),
                }).ToList(),
            });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"ResolveAudience: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}/preview")]
    public async Task<IActionResult> Preview(string id, [FromBody] PreviewRequest? request)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            var previews = await _emailCampaignApplicationService.PreviewCampaign(
                parsedId, request?.CapabilityIds);
            return Ok(new PreviewResponse
            {
                Previews = previews.Select(p => new PreviewItem
                {
                    CapabilityId = p.CapabilityId,
                    CapabilityName = p.CapabilityName,
                    Subject = p.Subject,
                    Html = p.Html,
                }).ToList(),
            });
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"PreviewEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}/send")]
    public async Task<IActionResult> Send(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            var result = await _emailCampaignApplicationService.SendCampaign(parsedId, userId.ToString());
            return Ok(result);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"SendEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}/schedule")]
    public async Task<IActionResult> Schedule(string id, [FromBody] ScheduleRequest request)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        if (!EmailCampaignScheduleType.TryParse(request.ScheduleType, out var scheduleType) ||
            scheduleType == EmailCampaignScheduleType.Immediate)
        {
            return BadRequest(
                new ProblemDetails { Title = "Invalid Request", Detail = "ScheduleType must be 'Scheduled' or 'Recurring'." }
            );
        }

        try
        {
            await _emailCampaignApplicationService.ScheduleCampaign(
                parsedId,
                scheduleType,
                request.ScheduledAt,
                request.CronExpression,
                userId.ToString()
            );
            var campaign = await _emailCampaignApplicationService.GetById(parsedId);
            return Ok(ToApiResource(campaign!));
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"ScheduleEmailCampaign: {e.Message}." }
            );
        }
    }

    [HttpGet("{id:required}/executions")]
    public async Task<IActionResult> GetExecutions(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            var executions = await _emailCampaignApplicationService.GetExecutions(parsedId);
            return Ok(executions.Select(ToExecutionApiResource));
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetExecutions: {e.Message}." }
            );
        }
    }

    [HttpGet("{id:required}/recipients")]
    public async Task<IActionResult> GetRecipients(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            var recipients = await _emailCampaignApplicationService.GetRecipientLog(parsedId);
            return Ok(recipients.Select(ToRecipientLogApiResource));
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetRecipients: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}/retry-failed")]
    public async Task<IActionResult> RetryFailed(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            var result = await _emailCampaignApplicationService.RetryFailedRecipients(parsedId, userId.ToString());
            return Ok(result);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"RetryFailedRecipients: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        if (!IsAuthorized())
            return Unauthorized();

        if (ParseCampaignId(id, out var parsedId) is { } parseError) return parseError;

        try
        {
            await _emailCampaignApplicationService.CancelCampaign(parsedId);
            return NoContent();
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = e.Message });
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"CancelEmailCampaign: {e.Message}." }
            );
        }
    }

    private static EmailCampaignApiResource ToApiResource(EmailCampaign b) =>
        new()
        {
            Id = b.Id.ToString(),
            Name = b.Name,
            Subject = b.Subject,
            ContentJson = b.ContentJson,
            ContentHtml = b.ContentHtml,
            AudienceJson = b.AudienceJson,
            RecipientFilter = b.RecipientFilter,
            ScheduleType = b.ScheduleType,
            ScheduledAt = b.ScheduledAt,
            CronExpression = b.CronExpression,
            Status = b.Status,
            CreatedAt = b.CreatedAt,
            CreatedBy = b.CreatedBy,
            ModifiedAt = b.ModifiedAt,
            ModifiedBy = b.ModifiedBy,
            SentAt = b.SentAt,
            CancelledAt = b.CancelledAt,
        };

    private static EmailCampaignExecutionApiResource ToExecutionApiResource(EmailCampaignExecution e) =>
        new()
        {
            Id = e.Id.ToString(),
            EmailCampaignId = e.EmailCampaignId.ToString(),
            ExecutedAt = e.ExecutedAt,
            TotalRecipients = e.TotalRecipients,
            SuccessCount = e.SuccessCount,
            FailureCount = e.FailureCount,
            Status = e.Status,
        };

    private static EmailCampaignRecipientLogApiResource ToRecipientLogApiResource(EmailCampaignRecipientLog r) =>
        new()
        {
            Id = r.Id.ToString(),
            EmailCampaignId = r.EmailCampaignId.ToString(),
            CapabilityId = r.CapabilityId,
            CapabilityName = r.CapabilityName,
            UserId = r.UserId,
            Email = r.Email,
            Status = r.Status,
            SentAt = r.SentAt,
            ErrorMessage = r.ErrorMessage,
            CreatedAt = r.CreatedAt,
        };
}
