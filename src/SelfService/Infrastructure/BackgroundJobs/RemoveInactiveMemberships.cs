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
    // private string authToken;
    // public Tuple<List<string>, List<string>> GetInactiveUsers(List<string> usersList, bool localTest = false)
    // /*
    //     Iterate over list of users, makes an ms-graph request for each of them to check their status
    // */
    // {
    //     List<string> inactiveNotFound = new List<string>();
    //     List<string> inactiveDisabled = new List<string>();

    //     if (localTest)
    //     {
    //         usersList = new List<string>(File.ReadAllLines("usernames.txt"));
    //     }

    //     foreach (string u in usersList)
    //     {
    //         string url = $"https://graph.microsoft.com/v1.0/users/{u}?%24select=displayName,accountEnabled,id,identities,mail";

    //         HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    //         request.Headers["Authorization"] = "Bearer " + authToken;
    //         request.Method = "GET";

    //         try
    //         {
    //             HttpWebResponse response = (HttpWebResponse)request.GetResponse();
    //             if (response.StatusCode == HttpStatusCode.OK)
    //             {
    //                 using (StreamReader sr = new StreamReader(response.GetResponseStream()))
    //                 {
    //                     string result = sr.ReadToEnd();
    //                     Console.WriteLine(result);
    //                     var usrJson = JsonSerializer.Deserialize<User>(result);
    //                     Console.WriteLine(usrJson.DisplayName+"\n");
    //                     if (!usrJson.AccountEnabled)
    //                     {
    //                         inactiveDisabled.Add(u);
    //                     }
    //                 }
    //             }
    //         }
    //         catch (WebException e)
    //         {
    //             if (e.Status == WebExceptionStatus.ProtocolError)
    //             {
    //                 HttpWebResponse httpResponse = (HttpWebResponse)e.Response;
    //                 if (httpResponse.StatusCode == HttpStatusCode.NotFound)
    //                 {
    //                     inactiveNotFound.Add(u);
    //                 }
    //                 else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
    //                 {
    //                     Console.WriteLine("Bad users (ms-graph) authorization token, exiting");
    //                     throw new Exception("Bad token");
    //                 }
    //             }
    //         }
    //     }
    //     return new Tuple<List<string>, List<string>>(inactiveNotFound, inactiveDisabled);
    // }

    // public Dictionary<string, string> UserToCauseMap(List<string> four04List, List<string> deactList)
    // {
    //     Dictionary<string, string> map = new Dictionary<string, string>();
    //     foreach (string u in four04List)
    //     {
    //         map[u] = "404";
    //     }
    //     foreach (string u in deactList)
    //     {
    //         map[u] = "deactivated";
    //     }

    //     return map;
    // }
}

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