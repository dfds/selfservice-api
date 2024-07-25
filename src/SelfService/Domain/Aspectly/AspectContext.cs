using System.Reflection;

namespace SelfService.Domain.Aspectly;

public class AspectContext
{
    public AspectContext(MethodInfo method, Attribute triggerAttribute)
    {
        Method = method;
        TriggerAttribute = triggerAttribute;
    }

    public Attribute TriggerAttribute { get; }
    public MethodInfo Method { get; }
}
