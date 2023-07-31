using System.Collections;

namespace SelfService.Infrastructure.Api;

[Flags]
public enum Method
{
    None,
    Get,
    Post,
    Put,
    Delete,
    Patch,
}

public class Allow : IEnumerable<string>
{
    public static Allow None => new();
    public static Allow Get => new() { Method.Get };
    public static Allow Post => new() { Method.Post };
    public static Allow Put => new() { Method.Put };

    private readonly HashSet<string> _allowed = new();

    public void Add(Method method)
    {
        _allowed.Add(method.ToString("G").ToUpper());
    }

    public IEnumerator<string> GetEnumerator()
    {
        return _allowed.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _allowed.GetEnumerator();
    }

    public static Allow operator +(Allow allow, Method method)
    {
        allow.Add(method);
        return allow;
    }
}
