using System.Reflection;

namespace SelfService.Domain.Models;

/// <summary>
/// In order to convert between objects that inherits from this this class you
/// MUST have a constructor that takes a Guid, ideally private or protected
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ValueObjectGuid<T> : ValueObject
    where T : ValueObjectGuid<T>
{
    private Guid Id { get; }

    protected virtual string Formatter => "D";

    /// <summary>
    /// Must be called from a constructor with signature (Guid newGuid)
    /// </summary>
    protected ValueObjectGuid(Guid newGuid)
    {
        Id = newGuid;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }

    public override string ToString()
    {
        return Id.ToString(Formatter);
    }

    /// <summary>
    /// This function lets us use reflection to construct an object of type T.
    /// Activator.CreateInstance can only be used to construct objects with a public constructor, which we do not want.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private static T UseReflectionToConstructObject(Guid guid)
    {
        try
        {
            var constructorInfo = typeof(T).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new Type[] { typeof(Guid) },
                null
            );

            if (constructorInfo != null)
            {
                return (T)constructorInfo.Invoke(new object[] { guid });
            }

            throw new InvalidOperationException($"No suitable constructor found for type {typeof(T).Name}");
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Unable to create an instance of type {typeof(T).Name}");
        }
    }

    public static T New()
    {
        return UseReflectionToConstructObject(Guid.NewGuid());
    }

    public static T NewFrom(Guid guid)
    {
        return UseReflectionToConstructObject(guid);
    }

    public static T Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid guid.");
    }

    public static bool TryParse(string? text, out T id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = (T)UseReflectionToConstructObject(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator ValueObjectGuid<T>(string text) => Parse(text);

    public static implicit operator string(ValueObjectGuid<T> id) => id.ToString();

    public static implicit operator Guid(ValueObjectGuid<T> id) => id.Id;

    public static implicit operator ValueObjectGuid<T>(Guid idValue) => UseReflectionToConstructObject(idValue);
}
