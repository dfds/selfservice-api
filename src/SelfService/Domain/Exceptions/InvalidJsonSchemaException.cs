using System.Text;
using Json.Schema;

namespace SelfService.Domain.Exceptions;

public class InvalidJsonSchemaException : Exception
{
    private static string ErrorDictionaryToString(IReadOnlyDictionary<string, string>? errors)
    {
        if (errors == null)
            return "";

        StringBuilder s = new StringBuilder();
        foreach (var keyValuePair in errors)
        {
            s.AppendLine($"{keyValuePair.Key}: {keyValuePair.Value}");
        }

        return s.ToString();
    }

    public InvalidJsonSchemaException(EvaluationResults result)
        : base($"Invalid Json Schema, errors: {(result.HasErrors ? ErrorDictionaryToString(result.Errors) : "")}") { }
}
