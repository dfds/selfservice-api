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
        List<Member> deactivatedMembers = new List<Member>();
        foreach (var m in members)
        {
            _logger.LogDebug($"member: {m}");
            if (IsDeactivated(m.Id)){
               _logger.LogDebug($"user {m.Id} is deactivated"); //TODO: remove
               deactivatedMembers.Add(m);
            }
        }

        foreach (var m in deactivatedMembers){
            var impactedMemberships = _context.Memberships.Where(x => x.UserId == m.Id);
            foreach (var membership in impactedMemberships){
                _logger.LogDebug($"removing membership {membership.Id} of deactivated user {membership.UserId}"); //TODO: remove
                _context.Remove(membership);
            }
        }
        _logger.LogDebug("pushing changes to db...");
        _context.SaveChanges(); // TODO: find out if should be async (?)
        _logger.LogDebug("Done.");

    }

    private Task<List<Member>> FetchAllMembers()
    {
        _logger.LogDebug($"members list: \n {_context.Members.ToList()}");
        return _context.Members.ToListAsync();
    }

    private bool IsDeactivated(string userId){
        bool b;
        (b, _) = userStatusCheck.MakeUserRequest(userId);
        return b;
    }


}