using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using SelfService.Domain.Exceptions;

namespace SelfService.Domain.Models;

public class MessageContractSchema : ValueObject
{
    private readonly string _value;
    public int Version { get; private set; }

    private MessageContractSchema(string value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static MessageContractSchema Parse(string? text)
    {
        if (TryParse(text, out var schema))
        {
            return schema;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public void CheckValidSchemaEnvelope()
    {
        CheckIsValidJsonSchema();

        var jsonNode = JsonNode.Parse(_value)!.AsObject();
        if (!jsonNode.TryGetPropertyValue("required", out var requiredPropertiesNode))
            throw new FormatException($"Value \"{_value}\" is not valid, missing required key \"required\".");

        var requiredKeys = requiredPropertiesNode?.AsArray().Select(x => x!.ToString()).ToList();
        if (requiredKeys == null)
            throw new FormatException($"Value \"{_value}\" is not valid, missing required key \"required\".");

        string[] mandatoryEnvelopeKeys = { "schemaVersion", "type", "messageId" };
        foreach (var key in mandatoryEnvelopeKeys)
        {
            if (!requiredKeys.Contains(key))
            {
                throw new FormatException($"Value \"{_value}\" is not valid, missing required key \"{key}\".");
            }
        }

        jsonNode.TryGetPropertyValue("properties", out var propertiesNode);
        var properties = propertiesNode?.AsObject();
        if (properties == null)
            throw new FormatException($"Value \"{_value}\" is not valid, missing required key \"properties\".");
        var asObject = propertiesNode!.AsObject();
        EnsurePropertyOfType(asObject, "schemaVersion", "integer");
        EnsureSchemaIsConst(asObject);
        EnsurePropertyOfType(asObject, "type", "string");
        EnsurePropertyOfType(asObject, "messageId", "string");
    }

    private void EnsurePropertyOfType(JsonObject propertiesNode, string propertyName, string type)
    {
        if (!propertiesNode.TryGetPropertyValue(propertyName, out var propertyNode))
            throw new FormatException($"Value \"{_value}\" is not valid, missing required key \"{propertyName}\".");
        if (propertyNode?.AsObject().TryGetPropertyValue("type", out var typeNode) != true)
            throw new FormatException(
                $"Value \"{_value}\" is not valid, missing required key \"type\" for property \"{propertyName}\"."
            );
        if (typeNode?.ToString() != type)
            throw new FormatException(
                $"Value \"{_value}\" is not valid, property \"{propertyName}\" must be of type \"{type}\"."
            );
    }

    private void EnsureSchemaIsConst(JsonObject propertiesNode)
    {
        if (!propertiesNode.TryGetPropertyValue("schemaVersion", out var schemaVersionNode))
            throw new FormatException($"Value \"{_value}\" is not valid, missing required key \"schemaVersion\".");
        if (schemaVersionNode?.AsObject().TryGetPropertyValue("const", out _) != true)
            throw new FormatException(
                $"Value \"{_value}\" is not valid, missing required key \"enum\" for property \"schemaVersion\"."
            );
    }

    private void CheckIsValidJsonSchema()
    {
        var jsonNode = JsonNode.Parse(_value)!.AsObject();
        var result = MetaSchemas.Content202012.Evaluate(
            jsonNode,
            new EvaluationOptions { ValidateAgainstMetaSchema = true, OutputFormat = OutputFormat.Hierarchical }
        );
        if (!result.IsValid)
            throw new InvalidJsonSchemaException(result);

        // Check if json can be parsed
        JsonSchema.FromText(_value);
    }

    public int? GetSchemaVersion()
    {
        JsonDocument asDocument = JsonDocument.Parse(_value);
        try
        {
            return asDocument.RootElement
                .GetProperty("properties")
                .GetProperty("schemaVersion")
                .GetProperty("const")
                .GetInt32();
        }
        catch
        {
            return null;
        }
    }

    public static bool TryParse(string? text, out MessageContractSchema schema)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            schema = null!;
            return false;
        }

        // NOTE: [jandr] consider having opinions about it being json

        schema = new MessageContractSchema(text!);
        return true;
    }

    public static implicit operator MessageContractSchema(string text) => Parse(text);

    public static implicit operator string(MessageContractSchema schema) => schema.ToString();
}
