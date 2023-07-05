using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.BackgroundJobs;
public class InactiveMembershipCleaner
{
    private readonly SelfServiceDbContext _context;

    public InactiveMembershipCleaner(SelfServiceDbContext context)
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