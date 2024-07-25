namespace SelfService.Domain.Aspectly;

public interface IAspect
{
    Task Invoke(AspectContext context, AspectDelegate next);
}

public delegate Task AspectDelegate();
