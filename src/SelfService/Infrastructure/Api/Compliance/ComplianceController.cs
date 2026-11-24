using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace SelfService.Infrastructure.Api.Compliance;

[Route("compliance")]
[Produces("application/json")]
[ApiController]
[SwaggerTag(
    "Capability compliance with the platform requirements per capability, cost centre and requirement. Today (UTC) is "
        + "computed live; earlier days are read from daily snapshots."
)]
public class ComplianceController : ControllerBase
{
    private const string DateDescription =
        "UTC day as YYYY-MM-DD. Omitted or today: live data. An earlier day: that day's snapshot, served with "
        + "Cache-Control: private, max-age=86400.";
    private const string DateBadRequest =
        "date is after today (UTC), or is not a date. An unparseable date returns ValidationProblemDetails "
        + "with an errors map.";
    private const string DateNotFound = "No snapshot exists for date, or date is more than 1100 days ago.";
    private const string ErrorContentType = "application/json";

    public const int DefaultCapabilityHistoryDays = 90;

    // A past day's snapshot is rewritten only until midnight UTC; after that it never changes.
    private const string HistoricalCacheControl = "private, max-age=86400";

    private readonly IComplianceApplicationService _complianceService;
    private readonly SystemTime _systemTime;

    public ComplianceController(IComplianceApplicationService complianceService, SystemTime systemTime)
    {
        _complianceService = complianceService;
        _systemTime = systemTime;
    }

    private DateOnly UtcToday => DateOnly.FromDateTime(_systemTime.Now);

    [HttpGet("summary")]
    [SwaggerOperation(
        Summary = "Get compliance totals for all capabilities",
        Description = "Counts active capabilities by overall status.",
        OperationId = "ComplianceGetSummary"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(ComplianceSummaryApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetComplianceSummary([FromQuery, SwaggerParameter(DateDescription)] DateOnly? date)
    {
        return ForDate(
            date,
            async d => Ok(ComplianceSummaryApiResource.From(await _complianceService.GetComplianceSummary(d)))
        );
    }

    [HttpGet("capabilities/{id}")]
    [SwaggerOperation(Summary = "Get a capability's compliance", OperationId = "ComplianceGetCapability")]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CapabilityComplianceApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "The capability does not exist, or is not in that day's snapshot. " + DateNotFound,
        typeof(ProblemDetails),
        ErrorContentType
    )]
    public Task<IActionResult> GetCapabilityCompliance(
        [FromRoute, SwaggerParameter("Capability id.")] string id,
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
            {
                try
                {
                    var capabilityId = CapabilityId.Parse(id);
                    var result = await _complianceService.GetCapabilityCompliance(capabilityId, d);
                    return Ok(CapabilityComplianceApiResource.From(result));
                }
                catch (EntityNotFoundException)
                {
                    return NotFound();
                }
            }
        );
    }

    [HttpGet("capabilities/{id}/history")]
    [SwaggerOperation(
        Summary = "Get a capability's daily compliance history",
        OperationId = "ComplianceGetCapabilityHistory"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CapabilityComplianceHistoryApiResource))]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "days is outside 1-1100, or is not an integer (ValidationProblemDetails).",
        typeof(ProblemDetails),
        ErrorContentType
    )]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "id is not a valid capability id.",
        typeof(ProblemDetails),
        ErrorContentType
    )]
    public async Task<IActionResult> GetCapabilityComplianceHistory(
        [FromRoute, SwaggerParameter("Capability id.")] string id,
        [FromQuery, SwaggerParameter("Number of days, 1-1100.")] int days = DefaultCapabilityHistoryDays
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (days < 1 || days > ComplianceRetention.Days)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid number of days",
                    Detail = $"days must be between 1 and {ComplianceRetention.Days}.",
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }

        if (!CapabilityId.TryParse(id, out var capabilityId))
        {
            return NotFound();
        }

        var today = UtcToday;
        var entries = await _complianceService.GetCapabilityHistory(capabilityId, today.AddDays(1 - days), today);
        return Ok(CapabilityComplianceHistoryApiResource.From(entries));
    }

    [HttpGet("cost-centres")]
    [SwaggerOperation(
        Summary = "Get compliance for every cost centre",
        Description = "One item per cost centre (matched case-insensitively), then the rogue group (no dfds.cost.centre tag) and the orphaned group (no members), which are always present. An orphaned capability is also counted in its cost centre or the rogue group.",
        OperationId = "ComplianceGetAllCostCentres"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(AllCostCentresComplianceApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetAllCostCentresCompliance(
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
                Ok(AllCostCentresComplianceApiResource.From(await _complianceService.GetAllCostCentresCompliance(d)))
        );
    }

    [HttpGet("cost-centres/{costCentre}")]
    [SwaggerOperation(
        Summary = "Get compliance for a cost centre",
        Description = "costCentre is matched case-insensitively. An unknown cost centre returns zero counts.",
        OperationId = "ComplianceGetCostCentre"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetCostCentreCompliance(
        [FromRoute, SwaggerParameter("Value of the dfds.cost.centre tag.")] string costCentre,
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
                Ok(
                    CostCentreComplianceApiResource.From(
                        await _complianceService.GetCostCentreCompliance(costCentre, d)
                    )
                )
        );
    }

    [HttpGet("cost-centres/{costCentre}/details")]
    [SwaggerOperation(
        Summary = "Get compliance for a cost centre, per capability",
        Description = "costCentre is matched case-insensitively. An unknown cost centre returns zero counts and no capabilities.",
        OperationId = "ComplianceGetCostCentreDetails"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceDetailsApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetCostCentreComplianceDetails(
        [FromRoute, SwaggerParameter("Value of the dfds.cost.centre tag.")] string costCentre,
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
                Ok(
                    CostCentreComplianceDetailsApiResource.From(
                        await _complianceService.GetCostCentreComplianceDetails(costCentre, d)
                    )
                )
        );
    }

    [HttpGet("rogue-capabilities")]
    [SwaggerOperation(
        Summary = "Get compliance for capabilities without a cost centre",
        Description = "Rogue capabilities have no dfds.cost.centre tag, or a blank one.",
        OperationId = "ComplianceGetRogueCapabilities"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetRogueCapabilitiesCompliance(
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
                Ok(CostCentreComplianceApiResource.From(await _complianceService.GetRogueCapabilitiesCompliance(d)))
        );
    }

    [HttpGet("rogue-capabilities/details")]
    [SwaggerOperation(
        Summary = "Get compliance for capabilities without a cost centre, per capability",
        Description = "Rogue capabilities have no dfds.cost.centre tag, or a blank one.",
        OperationId = "ComplianceGetRogueCapabilitiesDetails"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceDetailsApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetRogueCapabilitiesComplianceDetails(
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
                Ok(
                    CostCentreComplianceDetailsApiResource.From(
                        await _complianceService.GetRogueCapabilitiesComplianceDetails(d)
                    )
                )
        );
    }

    [HttpGet("orphaned-capabilities")]
    [SwaggerOperation(
        Summary = "Get compliance for capabilities without members",
        OperationId = "ComplianceGetOrphanedCapabilities"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetOrphanedCapabilitiesCompliance(
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
                Ok(CostCentreComplianceApiResource.From(await _complianceService.GetOrphanedCapabilitiesCompliance(d)))
        );
    }

    [HttpGet("orphaned-capabilities/details")]
    [SwaggerOperation(
        Summary = "Get compliance for capabilities without members, per capability",
        OperationId = "ComplianceGetOrphanedCapabilitiesDetails"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceDetailsApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetOrphanedCapabilitiesComplianceDetails(
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
                Ok(
                    CostCentreComplianceDetailsApiResource.From(
                        await _complianceService.GetOrphanedCapabilitiesComplianceDetails(d)
                    )
                )
        );
    }

    [HttpGet("requirements")]
    [SwaggerOperation(
        Summary = "Get compliance per requirement",
        Description = "Totals for each requirement, with a breakdown by cost centre.",
        OperationId = "ComplianceGetRequirements"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(RequirementsComplianceApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(StatusCodes.Status404NotFound, DateNotFound, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetRequirementsCompliance([FromQuery, SwaggerParameter(DateDescription)] DateOnly? date)
    {
        return ForDate(
            date,
            async d => Ok(RequirementsComplianceApiResource.From(await _complianceService.GetRequirementsCompliance(d)))
        );
    }

    [HttpGet("requirements/{id}")]
    [SwaggerOperation(
        Summary = "Get compliance for a requirement, per capability",
        OperationId = "ComplianceGetRequirement"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(RequirementComplianceDetailsApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, DateBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "id is not a known requirement. " + DateNotFound,
        typeof(ProblemDetails),
        ErrorContentType
    )]
    public Task<IActionResult> GetRequirementComplianceDetails(
        [FromRoute, SwaggerParameter("Requirement id.")] string id,
        [FromQuery, SwaggerParameter(DateDescription)] DateOnly? date
    )
    {
        return ForDate(
            date,
            async d =>
            {
                try
                {
                    var result = await _complianceService.GetRequirementComplianceDetails(id, d);
                    return Ok(RequirementComplianceDetailsApiResource.From(result));
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
            }
        );
    }

    [HttpGet("history")]
    [SwaggerOperation(Summary = "Get daily compliance totals", OperationId = "ComplianceGetHistory")]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(ComplianceHistoryApiResource))]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "from is after to, or from or to is not a date (ValidationProblemDetails).",
        typeof(ProblemDetails),
        ErrorContentType
    )]
    public async Task<IActionResult> GetComplianceHistory(
        [FromQuery, SwaggerParameter("First UTC day, YYYY-MM-DD.")] DateOnly? from,
        [FromQuery, SwaggerParameter("Last UTC day, YYYY-MM-DD.")] DateOnly? to
    )
    {
        if (InvalidRange(from, to) is { } invalid)
        {
            return invalid;
        }

        var result = await _complianceService.GetHistory(from, to);
        return Ok(ComplianceHistoryApiResource.From(result));
    }

    [HttpGet("cost-centres/{costCentre}/history")]
    [SwaggerOperation(
        Summary = "Get a cost centre's daily compliance totals, per requirement",
        Description = HistorySliceDescription
            + " costCentre is matched case-insensitively; an unknown cost "
            + "centre returns no items.",
        OperationId = "ComplianceGetCostCentreHistory"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceHistoryApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, HistoryBadRequest, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetCostCentreComplianceHistory(
        [FromRoute, SwaggerParameter("Value of the dfds.cost.centre tag.")] string costCentre,
        [FromQuery, SwaggerParameter("First UTC day, YYYY-MM-DD.")] DateOnly? from,
        [FromQuery, SwaggerParameter("Last UTC day, YYYY-MM-DD.")] DateOnly? to
    )
    {
        return CostCentreHistory(ComplianceCostCentreKind.CostCentre, costCentre, from, to);
    }

    [HttpGet("rogue-capabilities/history")]
    [SwaggerOperation(
        Summary = "Get daily compliance totals for capabilities without a cost centre, per requirement",
        Description = HistorySliceDescription,
        OperationId = "ComplianceGetRogueCapabilitiesHistory"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceHistoryApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, HistoryBadRequest, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetRogueCapabilitiesComplianceHistory(
        [FromQuery, SwaggerParameter("First UTC day, YYYY-MM-DD.")] DateOnly? from,
        [FromQuery, SwaggerParameter("Last UTC day, YYYY-MM-DD.")] DateOnly? to
    )
    {
        return CostCentreHistory(ComplianceCostCentreKind.Rogue, null, from, to);
    }

    [HttpGet("orphaned-capabilities/history")]
    [SwaggerOperation(
        Summary = "Get daily compliance totals for capabilities without members, per requirement",
        Description = HistorySliceDescription,
        OperationId = "ComplianceGetOrphanedCapabilitiesHistory"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(CostCentreComplianceHistoryApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, HistoryBadRequest, typeof(ProblemDetails), ErrorContentType)]
    public Task<IActionResult> GetOrphanedCapabilitiesComplianceHistory(
        [FromQuery, SwaggerParameter("First UTC day, YYYY-MM-DD.")] DateOnly? from,
        [FromQuery, SwaggerParameter("Last UTC day, YYYY-MM-DD.")] DateOnly? to
    )
    {
        return CostCentreHistory(ComplianceCostCentreKind.Orphaned, null, from, to);
    }

    [HttpGet("requirements/{id}/history")]
    [SwaggerOperation(
        Summary = "Get a requirement's daily compliance totals, per cost centre",
        Description = HistorySliceDescription
            + " Each item's costCentres lists the cost centres, then the "
            + "rogue and orphaned groups.",
        OperationId = "ComplianceGetRequirementHistory"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, null, typeof(RequirementComplianceHistoryApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, HistoryBadRequest, typeof(ProblemDetails), ErrorContentType)]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "id is not a known requirement.",
        typeof(ProblemDetails),
        ErrorContentType
    )]
    public async Task<IActionResult> GetRequirementComplianceHistory(
        [FromRoute, SwaggerParameter("Requirement id.")] string id,
        [FromQuery, SwaggerParameter("First UTC day, YYYY-MM-DD.")] DateOnly? from,
        [FromQuery, SwaggerParameter("Last UTC day, YYYY-MM-DD.")] DateOnly? to
    )
    {
        if (InvalidRange(from, to) is { } invalid)
        {
            return invalid;
        }

        try
        {
            var result = await _complianceService.GetRequirementHistory(id, from, to);
            return Ok(RequirementComplianceHistoryApiResource.From(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private const string HistorySliceDescription =
        "One item per day with a snapshot that has the group, oldest first, read from the same daily summaries "
        + "as /compliance/history. from defaults to the oldest snapshot and to defaults to today (UTC). "
        + "Summaries captured before per-group requirement totals were stored have empty breakdowns.";

    private const string HistoryBadRequest =
        "from is after to, or from or to is not a date (ValidationProblemDetails).";

    private async Task<IActionResult> CostCentreHistory(
        ComplianceCostCentreKind kind,
        string? costCentre,
        DateOnly? from,
        DateOnly? to
    )
    {
        if (InvalidRange(from, to) is { } invalid)
        {
            return invalid;
        }

        var result = await _complianceService.GetCostCentreHistory(kind, costCentre, from, to);
        return Ok(CostCentreComplianceHistoryApiResource.From(result));
    }

    private IActionResult? InvalidRange(DateOnly? from, DateOnly? to)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (from > to)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid date range",
                    Detail = "from must not be after to.",
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }

        return null;
    }

    /// <summary>
    /// Resolves the <c>date</c> query parameter and runs <paramref name="action"/> with it: <c>null</c> for
    /// live data (absent or today, UTC), otherwise the snapshot date. Future dates are 400; dates outside
    /// retention or without a snapshot are 404.
    /// </summary>
    private async Task<IActionResult> ForDate(DateOnly? date, Func<DateOnly?, Task<IActionResult>> action)
    {
        // ApiBehaviorOptions.SuppressModelStateInvalidFilter is set, so an unparseable date would
        // otherwise bind as null and return live data.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var today = UtcToday;
        if (date is null || date == today)
        {
            return await action(null);
        }

        if (date > today)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Date in the future",
                    Detail = $"{date:yyyy-MM-dd} is after today ({today:yyyy-MM-dd}, UTC).",
                    Status = StatusCodes.Status400BadRequest,
                }
            );
        }

        if (date < ComplianceRetention.OldestRetained(today))
        {
            return SnapshotNotFound(date.Value);
        }

        try
        {
            var result = await action(date);
            if (result is OkObjectResult)
            {
                Response.Headers.CacheControl = HistoricalCacheControl;
            }

            return result;
        }
        catch (ComplianceSnapshotNotFoundException)
        {
            return SnapshotNotFound(date.Value);
        }
    }

    private NotFoundObjectResult SnapshotNotFound(DateOnly date)
    {
        return NotFound(
            new ProblemDetails
            {
                Title = "No compliance snapshot",
                Detail = $"There is no compliance snapshot for {date:yyyy-MM-dd}.",
                Status = StatusCodes.Status404NotFound,
            }
        );
    }
}
