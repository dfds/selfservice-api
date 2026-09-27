using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.CopilotInsights;

namespace SelfService.Infrastructure.Api.Copilot;

public class CopilotApiResourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IAuthorizationService _authorizationService;

    public CopilotApiResourceFactory(
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        IAuthorizationService authorizationService
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _authorizationService = authorizationService;
    }

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext ?? throw new ApplicationException("Not in a http request context!");

    private static string GetNameOf<TController>()
        where TController : ControllerBase => typeof(TController).Name.Replace("Controller", "");

    public async Task<CopilotMeApiResource> ConvertMe(CopilotOwnUsage usage, DateOnly from, DateOnly to)
    {
        var canReadAll = await _authorizationService.CanReadAllCopilotUsage(HttpContext.User.ToPortalUser());
        return Convert(usage, from, to, MeLinks(canReadAll));
    }

    public CopilotMeApiResource ConvertUser(CopilotOwnUsage usage, string login, DateOnly from, DateOnly to) =>
        Convert(usage, from, to, UserLinks(login));

    public CopilotMeCreditsApiResource ConvertMeCredits(CopilotOwnUsage usage, DateOnly from, DateOnly to) =>
        ConvertCredits(usage, from, to, MeLinks(false));

    public CopilotMeCreditsApiResource ConvertUserCredits(
        CopilotOwnUsage usage,
        string login,
        DateOnly from,
        DateOnly to
    ) => ConvertCredits(usage, from, to, UserLinks(login));

    public CopilotUserSearchApiResource ConvertUserSearch(IReadOnlyList<CopilotIdentitySummaryDto>? users)
    {
        return new CopilotUserSearchApiResource
        {
            Items =
                users
                    ?.Select(u => new CopilotUserSummaryApiResource
                    {
                        Login = u.Login,
                        Name = u.Name,
                        PrimaryEmail = u.PrimaryEmail,
                        Seated = u.Seated,
                    })
                    .ToList() ?? new List<CopilotUserSummaryApiResource>(),
            Meta = new CopilotUserSearchMetaApiResource { CopilotAvailable = users is not null },
        };
    }

    private static CopilotMeApiResource Convert(CopilotOwnUsage usage, DateOnly from, DateOnly to, CopilotMeLinks links)
    {
        var bundle = usage.Bundle;
        return new CopilotMeApiResource
        {
            Mapped = usage.Mapped,
            UnmappedReason = usage.UnmappedReason,
            Login = bundle?.Identity.Login,
            Name = bundle?.Identity.Name,
            Seated = bundle?.Identity.Seated ?? false,
            Seats = bundle?.Seats.Select(MapSeat).ToList() ?? new List<CopilotSeatApiResource>(),
            Daily = bundle?.Daily.Select(MapDay).ToList() ?? new List<CopilotDayApiResource>(),
            Latest28d = bundle?.Latest28d is { } period ? MapPeriod(period) : null,
            Credits = bundle?.Credits is { } credits ? MapCredits(credits) : null,
            Budget = bundle?.Budget is { } budget
                ? new CopilotBudgetApiResource
                {
                    Amount = budget.Amount,
                    Consumed = budget.Consumed,
                    PreventFurtherUsage = budget.PreventFurtherUsage,
                }
                : null,
            Meta = MapMeta(usage, from, to),
            Links = links,
        };
    }

    private static CopilotMeCreditsApiResource ConvertCredits(
        CopilotOwnUsage usage,
        DateOnly from,
        DateOnly to,
        CopilotMeLinks links
    )
    {
        return new CopilotMeCreditsApiResource
        {
            Mapped = usage.Mapped,
            UnmappedReason = usage.UnmappedReason,
            Year = from.Year,
            Month = from.Month,
            Credits = usage.Bundle?.Credits is { } credits ? MapCredits(credits) : null,
            Meta = MapMeta(usage, from, to),
            Links = links,
        };
    }

    private string Href(string action, object? values = null) =>
        _linkGenerator.GetUriByAction(
            httpContext: HttpContext,
            action: action,
            controller: GetNameOf<CopilotController>(),
            values: values
        ) ?? "";

    private CopilotMeLinks MeLinks(bool canReadAll)
    {
        return new CopilotMeLinks
        {
            Self = new ResourceLink(Href(nameof(CopilotController.GetMe)), "self", Allow.Get),
            Credits = new ResourceLink(Href(nameof(CopilotController.GetMeCredits)), "related", Allow.Get),
            Users = canReadAll
                ? new ResourceLink(Href(nameof(CopilotController.SearchUsers)), "related", Allow.Get)
                : null,
        };
    }

    private CopilotMeLinks UserLinks(string login)
    {
        return new CopilotMeLinks
        {
            Self = new ResourceLink(Href(nameof(CopilotController.GetUser), new { login }), "self", Allow.Get),
            Credits = new ResourceLink(
                Href(nameof(CopilotController.GetUserCredits), new { login }),
                "related",
                Allow.Get
            ),
        };
    }

    private static CopilotMetaApiResource MapMeta(CopilotOwnUsage usage, DateOnly from, DateOnly to)
    {
        return new CopilotMetaApiResource
        {
            CopilotAvailable = usage.Available,
            GeneratedAt = usage.Meta?.GeneratedAt,
            Freshness = usage.Meta?.Freshness ?? new Dictionary<string, DateTime?>(),
            From = from,
            To = to,
            MatchedEmail = usage.Meta?.MatchedEmail,
        };
    }

    private static CopilotSeatApiResource MapSeat(CopilotSeatDto seat) =>
        new()
        {
            Scope = seat.Scope,
            OrgName = seat.OrgName,
            PlanType = seat.PlanType,
            CreatedAt = seat.CreatedAt,
            LastActivityAt = seat.LastActivityAt,
            LastActivityEditor = seat.LastActivityEditor,
            LastAuthenticatedAt = seat.LastAuthenticatedAt,
            PendingCancellationDate = seat.PendingCancellationDate,
        };

    private static CopilotDayApiResource MapDay(CopilotUserDayDto day) =>
        new()
        {
            Day = day.Day,
            Interactions = day.UserInitiatedInteractionCount,
            CodeGenerations = day.CodeGenerationActivityCount,
            CodeAcceptances = day.CodeAcceptanceActivityCount,
            LocSuggestedToAdd = day.LocSuggestedToAddSum,
            LocSuggestedToDelete = day.LocSuggestedToDeleteSum,
            LocAdded = day.LocAddedSum,
            LocDeleted = day.LocDeletedSum,
            AiCreditsUsed = day.AiCreditsUsed,
            UsedAgent = day.UsedAgent,
            UsedChat = day.UsedChat,
            UsedCli = day.UsedCli,
            UsedCodingAgent = day.UsedCopilotCodingAgent,
            UsedCloudAgent = day.UsedCopilotCloudAgent,
            Ides = day.Ides,
            Languages = day.Languages,
            Models = day.Models,
        };

    private static CopilotPeriodApiResource MapPeriod(CopilotUserPeriodDto period) =>
        new()
        {
            From = period.From,
            To = period.To,
            DaysActive = period.Totals?.DaysActive ?? 0,
            FirstDay = period.Totals?.FirstDay,
            LastDay = period.Totals?.LastDay,
            Totals = period.Totals?.Totals ?? new(),
            Breakdowns = period.Breakdowns.ToDictionary(
                b => b.Key,
                b =>
                    (IReadOnlyList<CopilotFactApiResource>)
                        b
                            .Value.Select(f => new CopilotFactApiResource
                            {
                                Key1 = f.Key1,
                                Key2 = f.Key2,
                                Metrics = f.Metrics,
                            })
                            .ToList()
            ),
        };

    private static CopilotCreditsApiResource MapCredits(CopilotUserCreditsDto credits) =>
        new()
        {
            Totals = new CopilotCreditAmountApiResource
            {
                Quantity = credits.Totals?.Quantity ?? 0,
                GrossAmount = credits.Totals?.GrossAmount ?? 0,
            },
            ByDay = credits
                .ByDay.Select(d => new CopilotCreditDayApiResource
                {
                    Day = d.Day,
                    Quantity = d.Totals.Quantity,
                    GrossAmount = d.Totals.GrossAmount,
                })
                .ToList(),
            ByModel = credits
                .ByModel.Select(m => new CopilotCreditModelApiResource
                {
                    Model = m.Model,
                    Quantity = m.Totals.Quantity,
                    GrossAmount = m.Totals.GrossAmount,
                })
                .ToList(),
        };
}
