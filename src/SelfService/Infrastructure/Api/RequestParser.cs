using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api;

public class MissingRequestParserException<TInput> : Exception
{
    public MissingRequestParserException(Type outputType)
        : base($"Unable to find a request parser for {typeof(TInput).Name} to {outputType.Name}") { }
}

public class RequestParserHelper<TInput, TOutput>
    where TOutput : ValueObject
{
    private Dictionary<Type, RequestParser<TInput, TOutput>> Parsers { get; } = new();
    private ModelStateDictionary ModelState { get; set; } = new();

    private RequestParser<TInput, TOutput> GetParser<O>(Type type)
        where O : TOutput
    {
        if (!Parsers.ContainsKey(type))
        {
            var missingParser = type.GetMethod(
                "Parse",
                BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static
            );
            if (missingParser == null)
                throw new MissingRequestParserException<TInput>(type);

            Ensure<O>(i => (O)missingParser.Invoke(null, new object[] { i! })!);
        }

        return Parsers[type];
    }

    public void Ensure<O>(Func<TInput, TOutput> converter)
        where O : TOutput
    {
        if (Parsers.ContainsKey(typeof(O)))
            return;
        Parsers.Add(typeof(O), new RequestParser<TInput, TOutput>(converter, typeof(O).Name));
    }

    private TOutput ParseInternal<O>(TInput i)
        where O : TOutput
    {
        var parser = GetParser<O>(typeof(O));
        return parser.TryParse(i, ModelState);
    }

    public O1 Parse<O1>(TInput i1)
        where O1 : TOutput
    {
        return (O1)ParseInternal<O1>(i1);
    }

    public (O1, O2) Parse<O1, O2>(TInput i1, TInput i2)
        where O1 : TOutput
        where O2 : TOutput
    {
        return ((O1)ParseInternal<O1>(i1), (O2)ParseInternal<O2>(i2));
    }

    public (O1, O2, O3) Parse<O1, O2, O3>(TInput i1, TInput i2, TInput i3)
        where O1 : TOutput
        where O2 : TOutput
        where O3 : TOutput
    {
        return ((O1)ParseInternal<O1>(i1), (O2)ParseInternal<O2>(i2), (O3)ParseInternal<O3>(i3));
    }

    public (O1, O2, O3, O4) Parse<O1, O2, O3, O4>(TInput i1, TInput i2, TInput i3, TInput i4)
        where O1 : TOutput
        where O2 : TOutput
        where O3 : TOutput
        where O4 : TOutput
    {
        return (
            (O1)ParseInternal<O1>(i1),
            (O2)ParseInternal<O2>(i2),
            (O3)ParseInternal<O3>(i3),
            (O4)ParseInternal<O4>(i4)
        );
    }

    public void SetModelStateParser(ModelStateDictionary modelStateDictionary)
    {
        ModelState = modelStateDictionary;
    }
}

public class RequestParser<TInput, TOutput>
{
    private Func<TInput, TOutput> Converter { get; }
    private string TOutputName { get; }

    public RequestParser(Func<TInput, TOutput> converter, string outputTypeName)
    {
        Converter = converter;
        TOutputName = outputTypeName;
    }

    public TOutput TryParse(TInput input, ModelStateDictionary modelStateDictionary)
    {
        try
        {
            var parsed = Converter(input);
            if (parsed != null)
                return (TOutput)parsed;
        }
        catch (Exception)
        {
            // Suppress warning, we know what we are doing
        }

        modelStateDictionary.AddModelError(
            $"{TOutputName}",
            $"Unable to parse {typeof(TInput).Name} \"{input}\" as {TOutputName}"
        );
        return default!;
    }
}

public static class RequestParserRegistry
{
    private static RequestParserHelper<string?, ValueObject> StringToValueObject { get; } = new();

    public static RequestParserHelper<string?, ValueObject> StringToValueParser(
        ModelStateDictionary modelStateDictionary
    )
    {
        StringToValueObject.SetModelStateParser(modelStateDictionary);
        return StringToValueObject;
    }

    public static void AddErrorIfNull(string? value, string valueName, ModelStateDictionary modelStateDictionary)
    {
        if (value != null)
            return;

        modelStateDictionary.AddModelError(valueName, $"Value {valueName} can not be null");
    }
}
