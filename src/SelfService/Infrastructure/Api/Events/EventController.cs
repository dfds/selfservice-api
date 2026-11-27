using Microsoft.AspNetCore.Mvc;
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

    public EventController(
        ApiResourceFactory apiResourceFactory,
        IEventService eventService,
        IAuthorizationService authorizationService
    )
    {
        _apiResourceFactory = apiResourceFactory;
        _eventService = eventService;
        _authorizationService = authorizationService;
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

        return Ok(await _apiResourceFactory.Convert(events));
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

        if (!await _authorizationService.CanCreateEvent(User.ToPortalUser()))
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

        try
        {
            var createdEvent = await _eventService.CreateEvent(eventModel, request, userId.ToString());
            return Ok(await _apiResourceFactory.Convert(createdEvent));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Request", Detail = exception.Message });
        }
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
            return Ok(await _apiResourceFactory.Convert(eventModel));
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

        if (!await _authorizationService.CanUpdateEvent(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to update events. Only cloud engineers can perform this action.",
                }
            );
        }

        try
        {
            await _eventService.UpdateEvent(id, request, userId.ToString());
            var updatedEvent = await _eventService.GetEventById(id);
            return Ok(await _apiResourceFactory.Convert(updatedEvent));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Request", Detail = exception.Message });
        }
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

        if (!await _authorizationService.CanDeleteEvent(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to delete events. Only cloud engineers can perform this action.",
                }
            );
        }

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

        return Ok(await _apiResourceFactory.ConvertUpcomingEvents(upcomingEvents, latestHeldEvent));
    }
}
