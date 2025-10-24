using SelfService.Domain;
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IMemberApplicationService
{
    Task RegisterUserProfile(UserId userId, string name, string email);
    Task RegisterLastSeen(UserId userId, DateTime lastSeen);
}

public class MemberApplicationService : IMemberApplicationService
{
    private readonly ILogger<MemberApplicationService> _logger;
    private readonly IMemberRepository _memberRepository;
    private readonly SystemTime _systemTime;

    public MemberApplicationService(
        ILogger<MemberApplicationService> logger,
        IMemberRepository memberRepository,
        SystemTime systemTime
    )
    {
        _logger = logger;
        _memberRepository = memberRepository;
        _systemTime = systemTime;
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterUserProfile(UserId userId, string name, string email)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} for {CurrentUser}",
            nameof(RegisterUserProfile),
            GetType().FullName,
            userId
        );

        var user = await _memberRepository.FindBy(userId);
        if (user == null)
        {
            user = Member.Register(userId, name, email, new UserSettings());
            await _memberRepository.Add(user);

            _logger.LogInformation("User {UserId} has been registered", userId);

            return;
        }

        user.Update(email, name);

        _logger.LogDebug("User {UserId} has been updated", userId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterLastSeen(UserId userId, DateTime lastSeen)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} for {CurrentUser}",
            nameof(RegisterLastSeen),
            GetType().FullName,
            userId
        );

        var user = await _memberRepository.FindBy(userId);
        if (user != null)
        {
            user.UpdateLastSeen(_systemTime.Now);
        }
    }
}
