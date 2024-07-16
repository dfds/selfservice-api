namespace SelfService.Domain.Aspectly;

internal class AspectOptionsBuilder : IAspectOptions
{
    private readonly LinkedList<AttributeAspectMap> _maps = new LinkedList<AttributeAspectMap>();

    public void Register<TAttribute, TAspect>()
        where TAttribute : Attribute
        where TAspect : class, IAspect
    {
        _maps.AddLast(new AttributeAspectMap(typeof(TAttribute), typeof(TAspect)));
    }

    public IEnumerable<AttributeAspectMap> Maps => _maps;
}
