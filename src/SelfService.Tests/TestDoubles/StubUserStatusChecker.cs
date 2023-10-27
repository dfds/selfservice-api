using Microsoft.Extensions.Logging;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Tests.TestDoubles;

public class StubUserStatusChecker : IUserStatusChecker
{
    // make it able to be set up with a constructor
    // with things you set from outside



    public Task<bool> TrySetAuthToken()
    {
        return Task.FromResult(true);
    }

    private Task<bool> BusyWait()
    {
        return new Task<bool>(() => true);
    }

    public Task<UserStatusCheckerStatus> CheckUserStatus(string userId)
    {
        if (userId == "userdeactivated@dfds.com")
            return Task.FromResult(UserStatusCheckerStatus.Deactivated);

        if (userId == "useractive@dfds.com")
            return Task.FromResult(UserStatusCheckerStatus.Found);

        return Task.FromResult(UserStatusCheckerStatus.NotFound); //undefined for this test
    }
}
