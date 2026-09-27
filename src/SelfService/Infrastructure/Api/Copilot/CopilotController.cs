using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Infrastructure.Api.RBAC;

namespace SelfService.Infrastructure.Api.Copilot;

[Route("copilot")]
[RbacConfig(nameof(RbacObjectType.Global), "id")]
[Produces("application/json")]
[ApiController]
public class CopilotController : ControllerBase
{
    private const int MaxRangeDays = 400;
    private const int DefaultRangeDays = 28;

    private const int MinSearchLength = 2;
    private const int MaxSearchLength = 100;
    private const int DefaultSearchLimit = 10;
    private const int MaxSearchLimit = 25;

    private static readonly Regex GitHubLogin = new("^[A-Za-z0-9-]{1,39}$", RegexOptions.Compiled);

    private static ProblemDetails InvalidLogin() =>
        new() { Title = "Invalid login", Detail = "login is not a GitHub login" };

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
    [ProducesResponseType(typeof(CopilotMeApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMe(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseRange(from, to, out var fromDay, out var toDay, out var problem))
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetOwnUsage(User, fromDay, toDay, cancellationToken);
        return Ok(await _apiResourceFactory.ConvertMe(usage, fromDay, toDay));
    }

    /// The caller's AI credits and their list price for one calendar month, per day and per model.
    [HttpGet("me/credits")]
    [RequiresPermission("copilot", "read-own")]
    [ProducesResponseType(typeof(CopilotMeCreditsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMeCredits(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseMonth(year, month, out var fromDay, out var toDay, out var problem))
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetOwnUsage(User, fromDay, toDay, cancellationToken);
        return Ok(_apiResourceFactory.ConvertMeCredits(usage, fromDay, toDay));
    }

    /// GitHub users whose login, name or any email contains <c>q</c>, best matches first.
    /// When copilot-insights is unreachable the answer is 200 with no items and
    /// <c>meta.copilotAvailable: false</c>.
    [HttpGet("users")]
    [RequiresPermission("copilot", "read-all")]
    [ProducesResponseType(typeof(CopilotUserSearchApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] int? limit,
        CancellationToken cancellationToken
    )
    {
        var term = q?.Trim() ?? "";
        if (term.Length is < MinSearchLength or > MaxSearchLength)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid query",
                    Detail = $"q must be {MinSearchLength}-{MaxSearchLength} characters",
                }
            );
        }

        var users = await _copilotService.SearchUsers(
            term,
            Math.Clamp(limit ?? DefaultSearchLimit, 1, MaxSearchLimit),
            cancellationToken
        );
        return Ok(_apiResourceFactory.ConvertUserSearch(users));
    }

    /// The same resource as <c>/copilot/me</c>, for any GitHub login. An unknown login answers
    /// 200 with <c>mapped: false</c> and <c>unmappedReason: user_not_found</c>.
    [HttpGet("users/{login}")]
    [RequiresPermission("copilot", "read-all")]
    [ProducesResponseType(typeof(CopilotMeApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUser(
        string login,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken
    )
    {
        if (!GitHubLogin.IsMatch(login))
        {
            return BadRequest(InvalidLogin());
        }
        if (!TryParseRange(from, to, out var fromDay, out var toDay, out var problem))
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetUserUsage(login, fromDay, toDay, cancellationToken);
        return Ok(_apiResourceFactory.ConvertUser(usage, login, fromDay, toDay));
    }

    /// The same resource as <c>/copilot/me/credits</c>, for any GitHub login.
    [HttpGet("users/{login}/credits")]
    [RequiresPermission("copilot", "read-all")]
    [ProducesResponseType(typeof(CopilotMeCreditsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserCredits(
        string login,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken
    )
    {
        if (!GitHubLogin.IsMatch(login))
        {
            return BadRequest(InvalidLogin());
        }
        if (!TryParseMonth(year, month, out var fromDay, out var toDay, out var problem))
        {
            return BadRequest(problem);
        }

        var usage = await _copilotService.GetUserUsage(login, fromDay, toDay, cancellationToken);
        return Ok(_apiResourceFactory.ConvertUserCredits(usage, login, fromDay, toDay));
    }

    /// <summary>
    /// Read-only passthrough to copilot-insights <c>/api/v1/{path}</c> with the query string
    /// unchanged. Upstream status and body are preserved; <c>meta.copilotAvailable</c> is added.
    /// When copilot-insights is unreachable the answer is 200 with
    /// <c>{"data":null,"meta":{"copilotAvailable":false}}</c>.
    /// </summary>
    [HttpGet("admin/{**path}")]
    [RequiresPermission("copilot", "read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAdmin(string? path, CancellationToken cancellationToken)
    {
        if (!CopilotAdminPaths.TryEscape(path, out var escaped))
        {
            return BadRequest(
                new ProblemDetails { Title = "Invalid path", Detail = "The path is not a copilot-insights read route." }
            );
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

    private static bool TryParseRange(
        string? from,
        string? to,
        out DateOnly fromDay,
        out DateOnly toDay,
        out ProblemDetails? problem
    )
    {
        problem = null;
        fromDay = default;
        toDay = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!string.IsNullOrEmpty(to) && !TryParseDay(to, out toDay))
        {
            problem = new ProblemDetails { Title = "Invalid date", Detail = "to must be YYYY-MM-DD" };
            return false;
        }

        fromDay = toDay.AddDays(1 - DefaultRangeDays);
        if (!string.IsNullOrEmpty(from) && !TryParseDay(from, out fromDay))
        {
            problem = new ProblemDetails { Title = "Invalid date", Detail = "from must be YYYY-MM-DD" };
            return false;
        }

        if (fromDay > toDay)
        {
            problem = new ProblemDetails { Title = "Invalid range", Detail = "from must not be after to" };
            return false;
        }

        if (toDay.DayNumber - fromDay.DayNumber + 1 > MaxRangeDays)
        {
            problem = new ProblemDetails
            {
                Title = "Invalid range",
                Detail = $"the range may span at most {MaxRangeDays} days",
            };
            return false;
        }

        return true;
    }

    private static bool TryParseMonth(
        int? year,
        int? month,
        out DateOnly fromDay,
        out DateOnly toDay,
        out ProblemDetails? problem
    )
    {
        problem = null;
        fromDay = toDay = default;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (y is < 2000 or > 2100 || m is < 1 or > 12)
        {
            problem = new ProblemDetails { Title = "Invalid month", Detail = "year must be 2000-2100 and month 1-12" };
            return false;
        }

        fromDay = new DateOnly(y, m, 1);
        toDay = fromDay.AddMonths(1).AddDays(-1);
        return true;
    }

    private static bool TryParseDay(string raw, out DateOnly day) =>
        DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);
}
