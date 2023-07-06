using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.BackgroundJobs;
public class InactiveMembershipCleaner
{
    private readonly SelfServiceDbContext _context;
    private readonly ILogger<RemoveInactiveMemberships> _logger;
    UserStatusChecker userStatusCheck;
    public InactiveMembershipCleaner(SelfServiceDbContext context, ILogger<RemoveInactiveMemberships> logger)
    {
        _context = context;
        _logger = logger;
        userStatusCheck = new UserStatusChecker(_context, _logger);
    }

    public async Task CleanupMemberships()
    {
        // fetch all members list
        var members = await FetchAllMembers(); // Task<thing> is a promise of thing, await it to get thing
        foreach (var m in members)
        {
            _logger.LogDebug($"member: {m}");
            //if (IsDeactivated(m.Id)){
            //    _logger.LogDebug($"user {m.Id} is deactivated");
            //}
            //take membership's name/email
            //check deactivated/ not found
            // IF deactivated
            // THEN :
                // [within a transaction: ]
                // - delete membership
            // _context.Memberships.where(x => x.UserId == m.UserId)
        }
        _logger.LogDebug("made it through the end of members list");
    }

    private Task<List<Member>> FetchAllMembers()
    {
        _logger.LogDebug($"members list: \n {_context.Members.ToList()}");
        return _context.Members.ToListAsync();
    }

    private bool IsDeactivated(string userId){
        bool b;
        string r;
        (b, r) = userStatusCheck.MakeUserRequest(userId);
        return b;
    }


}