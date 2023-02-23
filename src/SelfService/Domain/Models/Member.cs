namespace SelfService.Domain.Models;

public class Member : AggregateRoot<UserId>
{
    public Member(UserId id, string email, string? displayName) : base(id)
    {
        Email = email;
        DisplayName = displayName;
    }

    public string Email { get; private set; }
    public string? DisplayName { get; private set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}