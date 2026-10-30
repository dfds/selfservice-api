namespace SelfService.Domain.Models;

public class KubernetesAccessId : ValueObjectGuid<KubernetesAccessId>
{
    private KubernetesAccessId(Guid value)
        : base(value) { }
}
