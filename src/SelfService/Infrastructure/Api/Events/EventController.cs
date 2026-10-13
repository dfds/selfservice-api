using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Persistence;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Infrastructure.Api.Events;

[Route("events")]
[Produces("application/json")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IEventService _eventService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IContentImageService _contentImageService;

    public EventController(
        ApiResourceFactory apiResourceFactory,
        IEventService eventService,
        IAuthorizationService authorizationService,
        IContentImageService contentImageService
    )
    {
        _apiResourceFactory = apiResourceFactory;
        _eventService = eventService;
        _authorizationService = authorizationService;
        _contentImageService = contentImageService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(EventsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetEvents()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        var events = await _eventService.GetAllEvents();
        var resource = _apiResourceFactory.Convert(events);
        foreach (var eventResource in resource.Events)
        {
            await ResolveImagesAsync(eventResource);
        }

        return Ok(resource);
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(EventApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> CreateEvent([FromBody] EventCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanCreateEvent(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to create events. Only cloud engineers can perform this action.",
                }
            );
        }

        var eventModel = new Event(
            id: new EventId(),
            eventDate: request.EventDate,
            title: request.Title!,
            description: request.Description!,
            type: request.Type,
            createdBy: userId,
            createdAt: DateTime.UtcNow
        );

        var createdEvent = await _eventService.CreateEvent(eventModel);

        // Add attachments if provided
        if (request.Attachments != null && request.Attachments.Any())
        {
            foreach (var attachmentDto in request.Attachments)
            {
                var attachment = new EventAttachment(
                    id: new EventAttachmentId(),
                    eventId: createdEvent.Id,
                    url: attachmentDto.Url!,
                    attachmentType: attachmentDto.Type,
                    description: attachmentDto.Description,
                    createdAt: DateTime.UtcNow
                );
                await _eventService.AddAttachmentToEvent(createdEvent.Id, attachment);
                createdEvent.AddAttachment(attachment);
            }
        }

        return Ok(await ResolveImagesAsync(_apiResourceFactory.Convert(createdEvent)));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetEvent(EventId id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        try
        {
            var eventModel = await _eventService.GetEventById(id);
            return Ok(await ResolveImagesAsync(_apiResourceFactory.Convert(eventModel)));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails { Title = "Not Found", Detail = $"Event with id '{id}' was not found." }
            );
        }
    }

    [HttpPost("{id}")]
    [ProducesResponseType(typeof(EventApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> UpdateEvent(EventId id, [FromBody] EventUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanUpdateEvent(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to update events. Only cloud engineers can perform this action.",
                }
            );
        }

        if (request.Description is not null)
        {
            request.Description = await _contentImageService.RestoreReferencesAsync(
                ContentImageOwnerType.Event,
                (Guid)id,
                request.Description,
                HttpContext.RequestAborted
            );
        }

        await _eventService.UpdateEvent(id, request);

        // Handle attachments if provided in the request
        if (request.Attachments != null)
        {
            // Get current attachments for this event
            var currentEvent = await _eventService.GetEventById(id);
            var currentAttachments = currentEvent.Attachments.ToList();

            // Determine which attachment IDs are in the request (existing attachments to keep)
            var requestedAttachmentIds = request
                .Attachments.Where(a => a.Id is not null)
                .Select(a => a.Id!)
                .ToHashSet();

            // Delete attachments that are not in the request
            foreach (var currentAttachment in currentAttachments)
            {
                if (!requestedAttachmentIds.Contains(currentAttachment.Id))
                {
                    await _eventService.DeleteAttachment(currentAttachment.Id);
                }
            }

            // Update existing attachments (those with an ID still present)
            foreach (var attachmentDto in request.Attachments.Where(a => a.Id is not null && a.Url is not null))
            {
                await _eventService.UpdateAttachment(
                    attachmentDto.Id!,
                    attachmentDto.Url!,
                    attachmentDto.Type,
                    attachmentDto.Description
                );
            }

            // Add new attachments (those without an ID)
            foreach (var attachmentDto in request.Attachments.Where(a => a.Id is null))
            {
                var attachment = new EventAttachment(
                    id: new EventAttachmentId(),
                    eventId: id,
                    url: attachmentDto.Url!,
                    attachmentType: attachmentDto.Type,
                    description: attachmentDto.Description,
                    createdAt: DateTime.UtcNow
                );
                await _eventService.AddAttachmentToEvent(id, attachment);
            }
        }

        var updatedEvent = await _eventService.GetEventById(id);

        return Ok(await ResolveImagesAsync(_apiResourceFactory.Convert(updatedEvent)));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> DeleteEvent(EventId id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanDeleteEvent(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to delete events. Only cloud engineers can perform this action.",
                }
            );
        }

        await _contentImageService.DeleteForOwnerAsync(
            ContentImageOwnerType.Event,
            (Guid)id,
            HttpContext.RequestAborted
        );
        await _eventService.DeleteEvent(id);

        return NoContent();
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(UpcomingEventsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetUpcomingEvents()
    {
        var upcomingEvents = await _eventService.GetUpcomingEvents(limit: 5);
        var latestHeldEvent = await _eventService.GetLatestHeldEvent();

        var resource = _apiResourceFactory.ConvertUpcomingEvents(upcomingEvents, latestHeldEvent);
        foreach (var eventResource in resource.UpcomingEvents)
        {
            await ResolveImagesAsync(eventResource);
        }

        if (resource.LatestHeldEvent is not null)
        {
            await ResolveImagesAsync(resource.LatestHeldEvent);
        }

        return Ok(resource);
    }

    private async Task<EventApiResource> ResolveImagesAsync(EventApiResource resource)
    {
        if (ContentImageReference.FindIds(resource.Description).Count == 0)
        {
            return resource;
        }

        Response.Headers.CacheControl = "private, no-store";
        resource.Description = await _contentImageService.ResolveReferencesAsync(
            ContentImageOwnerType.Event,
            (Guid)resource.Id,
            resource.Description,
            HttpContext.RequestAborted
        );
        return resource;
    }
}
