using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Api.JsonSchema;

[Route("json-schema")]
[Produces("application/json")]
[ApiController]
public class SelfServiceJsonSchemaController : ControllerBase
{
    private readonly ISelfServiceJsonSchemaService _selfServiceJsonSchemaService;

    public SelfServiceJsonSchemaController(ISelfServiceJsonSchemaService selfServiceJsonSchemaService)
    {
        _selfServiceJsonSchemaService = selfServiceJsonSchemaService;
    }

    [HttpGet("{id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetSchema(
        string id,
        [FromQuery] int schemaVersion = ISelfServiceJsonSchemaService.LatestVersionNumber
    )
    {
        if (!SelfServiceJsonSchemaObjectId.TryParse(id, out var parsedObjectId))
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid object Id",
                    Detail =
                        $"{id} is not a valid object id, valid object ids are: {SelfServiceJsonSchemaObjectId.ValidTypesString()}"
                }
            );

        try
        {
            var selfServiceJsonSchema = await _selfServiceJsonSchemaService.GetSchema(parsedObjectId, schemaVersion);
            if (selfServiceJsonSchema == null)
            {
                return Ok(
                    new SelfServiceJsonSchema(ISelfServiceJsonSchemaService.LatestVersionNumber, parsedObjectId, "{}")
                );
            }

            return Ok(selfServiceJsonSchema);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetSchema: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> AddSchema(string id, [FromBody] AddSelfServiceJsonSchemaRequest? request)
    {
        string? xApiKey = HttpContext.Request.Headers["x-api-key"];
        if (string.IsNullOrEmpty(xApiKey))
        {
            return Unauthorized();
        }
        string? expected = Environment.GetEnvironmentVariable("SS_ADD_JSON_SCHEMA_API_KEY");
        if (string.IsNullOrEmpty(expected))
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails() { Title = "Missing environment variable", Detail = "Adding schemas not allowed" }
            );
        }
        if (xApiKey != expected)
        {
            return Unauthorized();
        }

        if (!SelfServiceJsonSchemaObjectId.TryParse(id, out var parsedObjectId))
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid object Id",
                    Detail =
                        $"{id} is not a valid object id, valid object ids are: {SelfServiceJsonSchemaObjectId.ValidTypesString()}"
                }
            );

        if (request?.Schema == null)
            return BadRequest(new ProblemDetails { Title = "Invalid Schema", Detail = "Schema in request is null" });

        if (!request.Schema.TryGetPropertyValue("$schema", out _))
        {
            return BadRequest(
                new ProblemDetails()
                {
                    Title = "Invalid Schema",
                    Detail = "Schema in request does not contain a $schema property"
                }
            );
        }

        try
        {
            Json.Schema.JsonSchema.FromText(request.Schema.ToJsonString());
        }
        catch (JsonException e)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid Schema",
                    Detail = $"Schema in request could not be parsed: {e.Message}"
                }
            );
        }

        try
        {
            var selfServiceJsonSchema = await _selfServiceJsonSchemaService.AddSchema(
                parsedObjectId,
                request.Schema.ToJsonString()
            );
            return Ok(selfServiceJsonSchema);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"AddSchema: {e.Message}." }
            );
        }
    }
}
