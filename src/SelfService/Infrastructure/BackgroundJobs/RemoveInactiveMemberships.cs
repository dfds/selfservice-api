using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.BackgroundJobs;

public class RemoveInactiveMemberships : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RemoveInactiveMemberships> _logger;

    public RemoveInactiveMemberships(IServiceProvider serviceProvider, ILogger<RemoveInactiveMemberships> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWork(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }, stoppingToken);
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        using var _ = _logger.BeginScope("{BackgroundJob} {CorrelationId}",
            nameof(RemoveInactiveMemberships), Guid.NewGuid());

        var membershipCleaner = scope.ServiceProvider.GetRequiredService<MembershipCleaner>();

        _logger.LogDebug("Removing inactive/deleted users' memberships...");
        await membershipCleaner.CleanupMemberships();
    }
}

public class MembershipCleaner
{
    private readonly SelfServiceDbContext _context;

    public MembershipCleaner(SelfServiceDbContext context)
    {
        _context = context;
    }

    public async Task CleanupMemberships()
    {
        // fetch all members list
        var members = await FetchAllMembers(); // Task<thing> is a promise of thing, await it to get thing
        foreach (var m in members)
        {
            //take membership's name/email
            //check deactivated/ not found
            // IF deactivated
            // THEN :
                // [within a transaction: ]
                // - delete membership
            // _context.Memberships.where(x => x.UserId == m.UserId)
        }

    }

    private Task<List<Member>> FetchAllMembers()
    {
        return _context.Members.ToListAsync();
    }

    private bool IsDeactivated(string userId){
        return false;
    }
}