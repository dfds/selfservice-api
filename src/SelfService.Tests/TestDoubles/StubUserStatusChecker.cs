using Microsoft.Extensions.Logging;
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

    private void SetAuthToken()
    {
        return; //so we can await it
    }

    private Task<bool> BusyWait()
    {
        return new Task<bool>(() => true);
    }

    public async Task<(bool, string)> CheckUserStatus(string userId)
    {
        await BusyWait();
        if (userId == "userdeactivated@dfds.com")
            return new(true, "Deactivated");

        if (userId == "useractive@dfds.com")
            return (false, "");

        return (true, "NotFound"); //undefined for this test
    }
}
