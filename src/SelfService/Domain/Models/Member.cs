namespace SelfService.Domain.Models;

public class Member : AggregateRoot<UserId>
{
    private Member()
        : base(default!)
    {
        Email = string.Empty;
        DisplayName = string.Empty;
        UserSettings = new UserSettings();
        Type = MemberType.User;
    }

    public Member(UserId id, string email, string? displayName, UserSettings settings)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        UserSettings = settings;
        Type = MemberType.User;
    }

    private Member(UserId id, string email, string? displayName, UserSettings settings, MemberType type)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        UserSettings = settings;
        Type = type;
    }

    public string Email { get; private set; }
    public string? DisplayName { get; private set; } // NOTE [jandr@2023-04-20]: consider renaming this to just "name"
    public UserSettings UserSettings { get; private set; }
    public MemberType Type { get; private set; }

    public DateTime? LastSeen { get; private set; }

    public bool ShouldReceiveEmail => Type == MemberType.User && !string.IsNullOrEmpty(Email);

    public void Update(string email, string? displayName)
    {
        if (Type == MemberType.ServicePrincipal && email != Email)
        {
            throw new InvalidOperationException(
                "Service principal email is derived from claims and cannot be updated."
            );
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException($"Value \"{email}\" is not valid for email.");
        }

        if (email == Email && displayName == DisplayName)
        {
            return;
        }

        Email = email;
        DisplayName = displayName;
    }

    public void UpdateServicePrincipalDisplayName(string? displayName)
    {
        if (Type != MemberType.ServicePrincipal)
        {
            throw new InvalidOperationException(
                "UpdateServicePrincipalDisplayName is only valid for service principal members."
            );
        }

        DisplayName = displayName;
    }

    public void UpdateUserSettings(UserSettings settings)
    {
        UserSettings = settings;
    }

    public void UpdateLastSeen(DateTime lastSeen)
    {
        LastSeen = lastSeen;
    }

    public override string ToString()
    {
        return Id.ToString();
    }

    public static Member Register(UserId id, string email, string? displayName, UserSettings? settings)
    {
        UserSettings newSettings = settings ?? new UserSettings();
        var member = new Member(id, email, displayName, newSettings);

        // NOTE [jandr@2023-04-20]: no domain events at the moment

        return member;
    }

    public static Member RegisterServicePrincipal(UserId id, string syntheticEmail, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(syntheticEmail))
        {
            throw new ArgumentException("Synthetic email must be provided for a service principal.");
        }

        return new Member(id, syntheticEmail, displayName, new UserSettings(), MemberType.ServicePrincipal);
    }
}
