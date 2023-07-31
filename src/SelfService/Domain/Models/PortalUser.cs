namespace SelfService.Domain.Models;

public class PortalUser : ValueObject
{
    public PortalUser(UserId id, IEnumerable<UserRole> roles)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }

    public UserId Id { get; private set; }
    public IEnumerable<UserRole> Roles { get; private set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Roles;
    }

    public override string ToString()
    {
        return $"{Id} as [{string.Join(",", Roles)}]";
    }
}
