namespace SelfService.Domain.Models;

public class KubernetesLink : ValueObject
{
    public static readonly KubernetesLink Unlinked = new();

    protected KubernetesLink() { }

    public KubernetesLink(string? @namespace, DateTime? linkedAt)
    {
        Namespace = @namespace;
        LinkedAt = linkedAt;
    }

    public string? Namespace { get; private set; }
    public DateTime? LinkedAt { get; private set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Namespace!;
        yield return LinkedAt!;
    }

    public override string ToString()
    {
        return LinkedAt is null ? "<incomplete>" : Namespace ?? "";
    }
}
