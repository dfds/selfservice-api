using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Infrastructure.Api.RBAC;
using Swashbuckle.AspNetCore.Annotations;

namespace SelfService.Infrastructure.Api.Copilot;

[Route("copilot")]
[RbacConfig(nameof(RbacObjectType.Global), "id")]
[Produces("application/json")]
[ApiController]
[SwaggerTag("GitHub Copilot seats, usage and AI credits from copilot-insights.")]
public class CopilotController : ControllerBase
{
    private const int MaxRangeDays = 400;
    private const int DefaultRangeDays = 28;

    private const int MinSearchLength = 2;
    private const int MaxSearchLength = 100;
    private const int DefaultSearchLimit = 10;
    private const int MaxSearchLimit = 25;

    private static readonly Regex GitHubLogin = new("^[A-Za-z0-9-]{1,39}$", RegexOptions.Compiled);

    private readonly ICopilotInsightsApplicationService _copilotService;
    private readonly CopilotApiResourceFactory _apiResourceFactory;

    public CopilotController(
        ICopilotInsightsApplicationService copilotService,
        CopilotApiResourceFactory apiResourceFactory
    )
    {
        _copilotService = copilotService;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("me")]
    [RequiresPermission("copilot", "read-own")]
    [SwaggerOperation(Summary = "Get the caller's Copilot usage", OperationId = "CopilotGetMe")]
    [SwaggerResponse(StatusCodes.Status200OK, "The caller's usage.", typeof(CopilotMeApiResource))]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "from or to is not YYYY-MM-DD, from is after to, or the range exceeds 400 days.",
        typeof(ProblemDetails),
        "application/json"
    )]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Plain text: Missing permission read-own.")]
    public async Task<IActionResult> GetMe(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken
    )
    {
        if (ParseRange(from, to, out var range) is { } problem)
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetOwnUsage(User, range.From, range.To, cancellationToken);
        return Ok(await _apiResourceFactory.ConvertMe(usage, range.From, range.To));
    }

    [HttpGet("me/credits")]
    [RequiresPermission("copilot", "read-own")]
    [SwaggerOperation(Summary = "Get the caller's AI credits usage per month", OperationId = "CopilotGetMeCredits")]
    [SwaggerResponse(StatusCodes.Status200OK, "The caller's credits.", typeof(CopilotMeCreditsApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "application/json")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Plain text: Missing permission read-own.")]
    public async Task<IActionResult> GetMeCredits(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken
    )
    {
        if (ParseMonth(year, month, out var range) is { } problem)
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetOwnUsage(User, range.From, range.To, cancellationToken);
        return Ok(_apiResourceFactory.ConvertMeCredits(usage, range.From, range.To));
    }

    [HttpGet("users")]
    [RequiresPermission("copilot", "read-all")]
    [SwaggerOperation(Summary = "Search GitHub users", OperationId = "CopilotSearchUsers")]
    [SwaggerResponse(StatusCodes.Status200OK, "The matching users", typeof(CopilotUserSearchApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "application/json")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Missing permission read-all")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] int? limit,
        CancellationToken cancellationToken
    )
    {
        var term = q?.Trim() ?? "";
        if (term.Length is < MinSearchLength or > MaxSearchLength)
        {
            return BadRequest(Invalid("Invalid query", $"q must be {MinSearchLength}-{MaxSearchLength} characters"));
        }

        var users = await _copilotService.SearchUsers(
            term,
            Math.Clamp(limit ?? DefaultSearchLimit, 1, MaxSearchLimit),
            cancellationToken
        );
        return Ok(_apiResourceFactory.ConvertUserSearch(users));
    }

    [HttpGet("users/{login}")]
    [RequiresPermission("copilot", "read-all")]
    [SwaggerOperation(Summary = "Get a GitHub user's Copilot usage", OperationId = "CopilotGetUser")]
    [SwaggerResponse(StatusCodes.Status200OK, "The user's usage.", typeof(CopilotMeApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "application/json")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Missing permission read-all")]
    public async Task<IActionResult> GetUser(
        string login,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken
    )
    {
        if (!GitHubLogin.IsMatch(login))
        {
            return BadRequest(Invalid("Invalid login", "login is not a GitHub login"));
        }
        if (ParseRange(from, to, out var range) is { } problem)
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetUserUsage(login, range.From, range.To, cancellationToken);
        return Ok(_apiResourceFactory.ConvertUser(usage, login, range.From, range.To));
    }

    [HttpGet("users/{login}/credits")]
    [RequiresPermission("copilot", "read-all")]
    [SwaggerOperation(Summary = "Get a GitHub user's AI credits for a month", OperationId = "CopilotGetUserCredits")]
    [SwaggerResponse(StatusCodes.Status200OK, "The user's credits.", typeof(CopilotMeCreditsApiResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "application/json")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Missing permission read-all")]
    public async Task<IActionResult> GetUserCredits(
        string login,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken
    )
    {
        if (!GitHubLogin.IsMatch(login))
        {
            return BadRequest(Invalid("Invalid login", "login is not a GitHub login"));
        }
        if (ParseMonth(year, month, out var range) is { } problem)
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetUserUsage(login, range.From, range.To, cancellationToken);
        return Ok(_apiResourceFactory.ConvertUserCredits(usage, login, range.From, range.To));
    }

    [HttpGet("admin/{**path}")]
    [RequiresPermission("copilot", "read-all")]
    [SwaggerOperation(
        Summary = "Passthrough API request to copilot-insights",
        Description = "GET passthrough to copilot-insights /api/v1/{path} with the query string "
            + "unchanged. The first path segment must be one of scopes, status, summary, reports, "
            + "teams, seats, billing, credits, budgets, users or identity. An upstream 2xx, 400 or 404 "
            + "keeps its status and body, with meta.copilotAvailable: true added to a JSON object body. "
            + "Any other upstream status, or no answer, gives the last good response for this path and "
            + "query from the past hour, or else 200 {\"data\":null,\"meta\":{\"copilotAvailable\":false}}.",
        OperationId = "CopilotGetAdmin"
    )]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "The upstream body, or data: null with meta.copilotAvailable: false.",
        typeof(object)
    )]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "path is empty, has an empty, '.', '..' or percent-encoded segment, or its root is not on the "
            + "list (ProblemDetails); or copilot-insights rejected the query (its own error body).",
        typeof(ProblemDetails),
        "application/json"
    )]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Missing permission read-all")]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "copilot-insights has no such route or resource; its own error body."
    )]
    public async Task<IActionResult> GetAdmin(string? path, CancellationToken cancellationToken)
    {
        if (!CopilotAdminPaths.TryEscape(path, out var escaped))
        {
            return BadRequest(Invalid("Invalid path", "The path is not a copilot-insights read route."));
        }

        var response = await _copilotService.GetAdmin(escaped, Request.QueryString.Value ?? "", cancellationToken);
        if (!response.Available)
        {
            return Ok(new { data = (object?)null, meta = new { copilotAvailable = false } });
        }

        return new ContentResult
        {
            Content = response.Body,
            ContentType = "application/json",
            StatusCode = response.StatusCode,
        };
    }

    private static ProblemDetails? ParseRange(string? from, string? to, out (DateOnly From, DateOnly To) range)
    {
        range = default;

        var toDay = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrEmpty(to) && !TryParseDay(to, out toDay))
        {
            return Invalid("Invalid date", "to must be YYYY-MM-DD");
        }

        var fromDay = toDay.AddDays(1 - DefaultRangeDays);
        if (!string.IsNullOrEmpty(from) && !TryParseDay(from, out fromDay))
        {
            return Invalid("Invalid date", "from must be YYYY-MM-DD");
        }

        if (fromDay > toDay)
        {
            return Invalid("Invalid range", "from must not be after to");
        }

        if (toDay.DayNumber - fromDay.DayNumber + 1 > MaxRangeDays)
        {
            return Invalid("Invalid range", $"the range may span at most {MaxRangeDays} days");
        }

        range = (fromDay, toDay);
        return null;
    }

    private static ProblemDetails? ParseMonth(int? year, int? month, out (DateOnly From, DateOnly To) range)
    {
        range = default;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (y is < 2000 or > 2100 || m is < 1 or > 12)
        {
            return Invalid("Invalid month", "year must be 2000-2100 and month 1-12");
        }

        var first = new DateOnly(y, m, 1);
        range = (first, first.AddMonths(1).AddDays(-1));
        return null;
    }

    private static ProblemDetails Invalid(string title, string detail) => new() { Title = title, Detail = detail };

    private static bool TryParseDay(string raw, out DateOnly day) =>
        DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);
}
