using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Tests.TestDoubles;

public class StubUserStatusChecker : IUserStatusChecker
{
    private List<UserId> _deactivatedUsers = new List<UserId>();
    private List<UserId> _activeUsers = new List<UserId>();

    public StubUserStatusChecker() { }

    public StubUserStatusChecker WithDeactivatedUser(UserId userId)
    {
        _deactivatedUsers.Add(userId);
        return this;
    }

    public StubUserStatusChecker WithActiveUser(UserId userId)
    {
        _activeUsers.Add(userId);
        return this;
    }

    public Task<bool> TrySetAuthToken()
    {
        return Task.FromResult(true);
    }

    public Task<UserStatusCheckerStatus> CheckUserStatus(UserId userId)
    {
        if (_deactivatedUsers.Contains(userId))
            return Task.FromResult(UserStatusCheckerStatus.Deactivated);

        if (_activeUsers.Contains(userId))
            return Task.FromResult(UserStatusCheckerStatus.Found);

        return Task.FromResult(UserStatusCheckerStatus.NotFound); //undefined for this test
    }
}
