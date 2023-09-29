using System.Text;
using System.Text.Json.Nodes;
using SelfService.Domain.Models;
using Json.Schema;

namespace SelfService.Domain.Services;

public class SelfServiceJsonSchemaService : ISelfServiceJsonSchemaService
{
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
            : base($"Invalid Json Schema, errors: {(result.HasErrors ? ErrorDictionaryToString(result.Errors) : "")}")
        { }
    }

    private const string EmptyJsonData = "{}";
    private const int EmptyJsonSchemaVersion = 0;

    private readonly ILogger<SelfServiceJsonSchemaService> _logger;
    private readonly ISelfServiceJsonSchemaRepository _selfServiceJsonSchemaRepository;

    public SelfServiceJsonSchemaService(
        ILogger<SelfServiceJsonSchemaService> logger,
        ISelfServiceJsonSchemaRepository selfServiceJsonSchemaRepository
    )
    {
        _logger = logger;
        _selfServiceJsonSchemaRepository = selfServiceJsonSchemaRepository;
    }

    public Task<SelfServiceJsonSchema?> GetLatestSchema(SelfServiceJsonSchemaObjectId objectId)
    {
        return _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
    }

    public Task<SelfServiceJsonSchema?> GetSchema(SelfServiceJsonSchemaObjectId objectId, int schemaVersion)
    {
        return _selfServiceJsonSchemaRepository.GetSchema(objectId, schemaVersion);
    }

    public void MustValidateJsonSchemaAgainstMetaSchema(string schema)
    {
        // Check if json is valid
        JsonNode? actualObj = JsonNode.Parse(schema);
        var result = MetaSchemas.Content202012.Evaluate(
            actualObj,
            new EvaluationOptions { ValidateAgainstMetaSchema = true, OutputFormat = OutputFormat.Hierarchical }
        );
        if (!result.IsValid)
            throw new InvalidJsonSchemaException(result);

        // Check if json can be parsed
        JsonSchema.FromText(schema);
    }

    [TransactionalBoundary]
    public async Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchemaObjectId objectId, string schema)
    {
        MustValidateJsonSchemaAgainstMetaSchema(schema);

        var latest = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        var latestVersionNumber = latest?.SchemaVersion ?? EmptyJsonSchemaVersion;

        var targetVersion = latestVersionNumber + 1;
        var newSchema = new SelfServiceJsonSchema(targetVersion, objectId, schema);
        _logger.LogInformation("Adding new SelfServiceJsonSchema to the database: {SelfServiceJsonSchema}", newSchema);
        return await _selfServiceJsonSchemaRepository.AddSchema(newSchema);
    }

    private static bool IsEmptyJsonData(string? json)
    {
        return string.IsNullOrEmpty(json) || json == EmptyJsonData;
    }

    public async Task<ValidateJsonMetadataResult> ValidateJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata
    )
    {
        var latestSchema = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);

        // if json metadata is not empty, we need to validate it against the latest schema
        if (!IsEmptyJsonData(requestJsonMetadata))
        {
            if (latestSchema == null)
                return ValidateJsonMetadataResult.CreateError(
                    "Json metadata from request is not empty, but no schema exists"
                );
            var notNullRequestJsonData = requestJsonMetadata!;

            var parsedJsonSchema = JsonSchema.FromText(latestSchema.Schema);
            JsonNode? actualObj = JsonNode.Parse(notNullRequestJsonData);
            var result = parsedJsonSchema.Evaluate(actualObj);
            if (!result.IsValid)
                return ValidateJsonMetadataResult.CreateError(
                    $"Json metadata from request is not valid against schema: {result.Details}"
                );

            return ValidateJsonMetadataResult.CreateSuccess(
                notNullRequestJsonData,
                latestSchema.SchemaVersion,
                ValidateJsonMetadataResultCode.SuccessValidJsonMetadata
            );
        }

        // if json metadata is empty, we can allow it if the latest schema has no required fields
        if (latestSchema == null)
            return ValidateJsonMetadataResult.CreateSuccess(
                EmptyJsonData,
                EmptyJsonSchemaVersion,
                ValidateJsonMetadataResultCode.SuccessNoSchema
            );

        // if json schema has no required fields, we can allow an empty json object
        var jsonSchema = JsonSchema.FromText(latestSchema.Schema);
        var requiredFields = jsonSchema.GetRequired();
        if (requiredFields != null && requiredFields.Any())
        {
            return ValidateJsonMetadataResult.CreateError("Invalid Json Metadata");
        }

        return ValidateJsonMetadataResult.CreateSuccess(
            EmptyJsonData,
            latestSchema.SchemaVersion,
            ValidateJsonMetadataResultCode.SuccessSchemaHasNoRequiredFields
        );
    }
}
