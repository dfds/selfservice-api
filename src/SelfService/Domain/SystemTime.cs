namespace SelfService.Domain;

public class SystemTime
{
    public static SystemTime Default = new SystemTime(() => DateTime.UtcNow);
    private readonly Func<DateTime> _provider;

    public SystemTime(Func<DateTime> provider)
    {
        _provider = provider;
    }

    public DateTime Now => _provider();
}
