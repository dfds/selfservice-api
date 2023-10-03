using Microsoft.Extensions.Logging;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Tests.TestDoubles;

public class StubUserStatusChecker : IUserStatusChecker
{
    public Task<bool> TrySetAuthToken()
    {
        return Task.FromResult(false);
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
