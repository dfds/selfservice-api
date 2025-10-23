using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Demos;

[Route("demos")]
[Produces("application/json")]
[ApiController]
public class DemosController : ControllerBase
    {
        private readonly ApiResourceFactory _apiResourceFactory;
        private readonly IDemosService _demosService;
        private readonly IDemoApplicationService _demoApplicationService;
        private readonly IAuthorizationService _authorizationService;

        public DemosController(ApiResourceFactory apiResourceFactory, IDemosService demosService, IDemoApplicationService demoApplicationService, IAuthorizationService authorizationService)
        {
            _apiResourceFactory = apiResourceFactory;
            _demosService = demosService;
            _demoApplicationService = demoApplicationService;
            _authorizationService = authorizationService;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(IEnumerable<Demo>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> GetDemos()
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized(
                    new ProblemDetails
                    {
                        Title = "Access Denied!",
                        Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                    }
                );
            }

            var demos = await _demosService.GetAllDemos();

            return Ok(_apiResourceFactory.Convert(demos));
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(Demo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> CreateDemo([FromBody] DemoCreateRequest request)
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
                        Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                    }
                );
            }

            var demo = new Demo(
                id: new DemoId(),
                recordingDate: request.RecordingDate,
                title: request.Title!,
                description: request.Description!,
                uri: request.Uri!,
                tags: request.Tags!,
                createdBy: userId,
                createdAt: DateTime.UtcNow,
                isActive: request.IsActive
            );

            var createdDemo = await _demosService.AddDemo(demo);

            return Ok(_apiResourceFactory.Convert(createdDemo));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Demo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> GetDemo(DemoId id)
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized(
                    new ProblemDetails
                    {
                        Title = "Access Denied!",
                        Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                    }
                );
            }

            var demo = await _demosService.GetDemoById(id);

            return Ok(_apiResourceFactory.Convert(demo));
        }

        [HttpPost("{id}")]
        [ProducesResponseType(typeof(Demo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> UpdateDemo(DemoId id, [FromBody] DemoUpdateRequest request)
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
                        Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                    }
                );
            }

            await _demosService.UpdateDemo(id, request);

            var updatedDemo = await _demosService.GetDemoById(id);

            return Ok(_apiResourceFactory.Convert(updatedDemo));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Demo), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> DeleteDemo(DemoId id)
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized(
                    new ProblemDetails
                    {
                        Title = "Access Denied!",
                        Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                    }
                );
            }

            await _demosService.DeleteDemo(id);

            return NoContent();
        }

        [HttpGet("signups")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<IActionResult> GetActiveSignups()
        {
            if (!User.TryGetUserId(out var principalId))
            {
                return Unauthorized(
                    new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
                );
            }

            var isCloudEngineer = _authorizationService.CanSynchronizeAwsECRAndDatabaseECR(User.ToPortalUser());
            if (!isCloudEngineer)
                return Unauthorized();

            return Ok(_apiResourceFactory.Convert(await _demoApplicationService.GetActiveSignups()));
        }
    }
