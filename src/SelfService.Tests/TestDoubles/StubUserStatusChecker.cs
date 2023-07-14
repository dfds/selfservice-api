using Microsoft.Extensions.Logging;
//using SelfService.Domain.Services;
using SelfService.Infrastructure.BackgroundJobs;


namespace SelfService.Tests.TestDoubles;

public class StubUserStatusChecker : IUserStatusChecker
{
    private readonly ILogger<RemoveDeactivatedMemberships> _logger; //depends on that background job
    private string? _authToken;

    public StubUserStatusChecker(ILogger<RemoveDeactivatedMemberships> logger)
    {
        _logger = logger;
        SetAuthToken();
    }

    private async void SetAuthToken()
    {
        return; //so we can await it
    }

    public async Task<(bool, string)> MakeUserRequest(string userId)
    {
        if (userId == "userdeactivated@dfds.com")
        {
            return (true, "deactivated");
        }
        else if (userId == "useractive@dfds.com")
        {
            return (false, "");
        }
        else{
            return (false, "undefined for this test");
        }
    }
}

