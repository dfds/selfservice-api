using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.BackgroundJobs;

public class PortalVisitAnalyzer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public PortalVisitAnalyzer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWork(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }, stoppingToken);
    }

    public static DateTime GetStartOfWeek(DateTime dateTime)
    {
        var now = dateTime.Date;

        return now.DayOfWeek switch
        {
            DayOfWeek.Monday => now.Subtract(TimeSpan.FromDays(0)),
            DayOfWeek.Tuesday => now.Subtract(TimeSpan.FromDays(1)),
            DayOfWeek.Wednesday => now.Subtract(TimeSpan.FromDays(2)),
            DayOfWeek.Thursday => now.Subtract(TimeSpan.FromDays(3)),
            DayOfWeek.Friday => now.Subtract(TimeSpan.FromDays(4)),
            DayOfWeek.Saturday => now.Subtract(TimeSpan.FromDays(5)),
            DayOfWeek.Sunday => now.Subtract(TimeSpan.FromDays(6)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PortalVisitAnalyzer>>();

        using var _ = logger.BeginScope("{BackgroundJob} {CorrelationId}",
            nameof(PortalVisitAnalyzer), Guid.NewGuid());

        logger.LogDebug("Starting portal visit analyzer...");

        var dbContext = scope.ServiceProvider.GetRequiredService<SelfServiceDbContext>();
        var systemTime = scope.ServiceProvider.GetRequiredService<SystemTime>();

        var windowBegin = GetStartOfWeek(systemTime.Now.ToUniversalTime());
        var windowEnd = systemTime.Now.ToUniversalTime();

        var visits = await dbContext.PortalVisits
            .Where(x => x.VisitedAt >= windowBegin && x.VisitedAt <= windowEnd)
            .ToListAsync(cancellationToken);

        var windowDataSet = visits
            .GroupBy(x => x.VisitedBy)
            .ToArray();

        var stats = new List<UserVisitStat>();

        foreach (var portalVisits in windowDataSet)
        {
            var visitor = portalVisits.Key;

            var orderedVisits = portalVisits
                .OrderBy(x => x.VisitedAt)
                .ToArray();

            var lastVisit = orderedVisits.Last();
            var properVisitsDuringWindow = RemoveVisitsTooCloseToEachOther(orderedVisits);

            stats.Add(new UserVisitStat(
                UserId: visitor,
                LastVisit: lastVisit.VisitedAt,
                TotalVisits: properVisitsDuringWindow.Count()
            ));
        }

        var memberApplicationService = scope.ServiceProvider.GetRequiredService<IMemberApplicationService>();

        // update last seen per user...!
        foreach (var (userId, lastVisit, _) in stats)
        {
            await memberApplicationService.RegisterLastSeen(userId, lastVisit);
        }

        // update top visitors list
        var topVisitorsDuringWindow = stats
            .OrderByDescending(x => x.TotalVisits)
            .Take(3);

        var memberRepository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var result = new List<TopVisitorsRepository.VisitorRecord>();
        var rank = 0;

        foreach (var stat in topVisitorsDuringWindow)
        {
            var user = await memberRepository.FindBy(stat.UserId);
            result.Add(new TopVisitorsRepository.VisitorRecord(
                Id: stat.UserId,
                Name: user?.DisplayName ?? "n/a",
                Rank: ++rank
            ));
        }

        var topVisitorsRepository = scope.ServiceProvider.GetRequiredService<TopVisitorsRepository>();
        topVisitorsRepository.Update(result);

        logger.LogInformation("Top visitors between {WindowStart} and {WindowEnd} amongst {VisitCount} visits is updated to be: {Visitors}",
            windowBegin.ToUniversalTime().ToString("O"), 
            windowEnd.ToUniversalTime().ToString("O"), 
            visits.Count, 
            string.Join(", ", result.Select(x => x.Id))
        );
    }

    private record UserVisitStat(UserId UserId, DateTime LastVisit, int TotalVisits);

    private IEnumerable<PortalVisit> RemoveVisitsTooCloseToEachOther(IEnumerable<PortalVisit> visits)
    {
        var result = new LinkedList<PortalVisit>();

        var lastVisit = DateTime.MinValue;
        var cutOff = TimeSpan.FromMinutes(2);

        foreach (var visit in visits)
        {
            var timeSince = visit.VisitedAt - lastVisit;
            if (timeSince > cutOff)
            {
                result.AddLast(visit);
                lastVisit = visit.VisitedAt;
            }
        }

        return result;
    }
}

public class TopVisitorsRepository
{
    private static readonly List<VisitorRecord> Records = new List<VisitorRecord>();

    public IEnumerable<VisitorRecord> GetAll() => Records;

    public void Update(IEnumerable<VisitorRecord> records)
    {
        Records.Clear();
        Records.AddRange(records);
    }

    public record VisitorRecord(UserId Id, string Name, int Rank);
}
