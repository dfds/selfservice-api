using Microsoft.Extensions.Logging;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Tests.TestDoubles;

public class StubUserStatusChecker : IUserStatusChecker
{
    private readonly ILogger<RemoveDeactivatedMemberships> _logger; //depends on that background job

    public StubUserStatusChecker(ILogger<RemoveDeactivatedMemberships> logger)
    {
        _logger = logger;
        SetAuthToken();
    }

    public bool TrySetAuthToken()
    {
        return false;
    }

    private void SetAuthToken()
    {
        return; //so we can await it
    }

    private Task<bool> BusyWait()
    {
        return new Task<bool>(() => true);
    }

    public async Task<UserStatusCheckerStatus> CheckUserStatus(string userId)
    {
        await BusyWait();
        if (userId == "userdeactivated@dfds.com")
            return UserStatusCheckerStatus.Deactivated;

        if (userId == "useractive@dfds.com")
            return UserStatusCheckerStatus.Found;

        return UserStatusCheckerStatus.NotFound; //undefined for this test
    }
}
