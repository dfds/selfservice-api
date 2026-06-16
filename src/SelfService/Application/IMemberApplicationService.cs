using SelfService.Domain;
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IMemberApplicationService
{
    Task RegisterUserProfile(UserId userId, string name, string email);
    Task RegisterLastSeen(UserId userId, DateTime lastSeen);
    Task RegisterServicePrincipal(UserId userId, string syntheticEmail, string? displayName);
    Task SyncServicePrincipalDisplayName(UserId userId, string? displayName);
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
            // Member.Register is (id, email, displayName) — keep the same arg order as the Update
            // path below so create and update agree. (Previously this passed (name, email), which
            // stored Email/DisplayName swapped until the next profile update healed it; members
            // provisioned from the RBAC admin page never log in, so the swap was permanent.)
            user = Member.Register(userId, email, name, new UserSettings());
            await _memberRepository.Add(user);

            _logger.LogInformation("User {UserId} has been registered", userId);

            return;
        }

        user.Update(email, name);

        _logger.LogDebug("User {UserId} has been updated", userId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterServicePrincipal(UserId userId, string syntheticEmail, string? displayName)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} for {CurrentUser}",
            nameof(RegisterServicePrincipal),
            GetType().FullName,
            userId
        );

        var existing = await _memberRepository.FindBy(userId);
        if (existing == null)
        {
            var member = Member.RegisterServicePrincipal(userId, syntheticEmail, displayName);
            await _memberRepository.Add(member);
            _logger.LogInformation("Service principal {UserId} has been registered", userId);
            return;
        }

        if (existing.Type == MemberType.ServicePrincipal && existing.DisplayName != displayName)
        {
            existing.UpdateServicePrincipalDisplayName(displayName);
            _logger.LogDebug("Service principal {UserId} display name updated", userId);
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task SyncServicePrincipalDisplayName(UserId userId, string? displayName)
    {
        var existing = await _memberRepository.FindBy(userId);
        if (existing == null || existing.Type != MemberType.ServicePrincipal)
        {
            return;
        }

        if (existing.DisplayName != displayName)
        {
            existing.UpdateServicePrincipalDisplayName(displayName);
        }
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
