using System.Text.Json;
using System.Text.Json.Nodes;
using SelfService.Domain.Models;
using Json.Schema;
using Json.Schema.DataGeneration;

namespace SelfService.Domain.Services;

public class SelfServiceJsonSchemaService : ISelfServiceJsonSchemaService
{
    private const string EmptyJsonData = "{}";

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

    public Task<SelfServiceJsonSchema?> GetSchema(
        SelfServiceJsonSchemaObjectId objectId,
        int schemaVersion = ISelfServiceJsonSchemaService.LatestVersionNumber
    )
    {
        return schemaVersion == ISelfServiceJsonSchemaService.LatestVersionNumber
            ? _selfServiceJsonSchemaRepository.GetLatestSchema(objectId)
            : _selfServiceJsonSchemaRepository.GetSchema(objectId, schemaVersion);
    }

    [TransactionalBoundary]
    public async Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchemaObjectId objectId, string schema)
    {
        var latest = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        var latestVersionNumber = latest?.SchemaVersion ?? ISelfServiceJsonSchemaService.LatestVersionNumber;

        var targetVersion = latestVersionNumber + 1;
        var newSchema = new SelfServiceJsonSchema(targetVersion, objectId, schema);
        _logger.LogInformation("Adding new SelfServiceJsonSchema to the database: {SelfServiceJsonSchema}", newSchema);
        return await _selfServiceJsonSchemaRepository.AddSchema(newSchema);
    }

    public async Task<JsonObject?> GetEmptyJsonDataObjectFromLatestSchema(SelfServiceJsonSchemaObjectId objectId)
    {
        var latestSchema = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        if (latestSchema == null)
        {
            return await Task.FromResult<JsonObject?>(null);
        }

        var jsonSchema = JsonSchema.FromText(latestSchema.Schema);
        var requiredFields = jsonSchema.GetRequired();
        if (requiredFields != null && requiredFields.Any())
        {
            return null;
        }

        return JsonObject.Create(new JsonElement());
    }

    public Task<bool> IsJsonDataValid(string jsonSchemaString, string jsonData)
    {
        var jsonSchema = JsonSchema.FromText(jsonSchemaString);
        JsonNode? actualObj = JsonNode.Parse(jsonData);
        var result = jsonSchema.Evaluate(actualObj);
        return Task.FromResult(result.IsValid);
    }

    public async Task<ParsedJsonMetadataResult> ParseJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata
    )
    {
        var latestSchema = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        if (!string.IsNullOrEmpty(requestJsonMetadata) && requestJsonMetadata != EmptyJsonData)
        {
            if (latestSchema == null)
                return ParsedJsonMetadataResult.CreateError(
                    "Json metadata from request is not empty, but no schema exists"
                );

            if (!await IsJsonDataValid(latestSchema.Schema, requestJsonMetadata))
                return ParsedJsonMetadataResult.CreateError("Json metadata from request is not valid against schema");

            return ParsedJsonMetadataResult.CreateSuccess(
                requestJsonMetadata,
                latestSchema.SchemaVersion,
                ParsedJsonMetadataResultCode.SuccessFromRequest
            );
        }

        if (latestSchema == null)
            return ParsedJsonMetadataResult.CreateSuccess(
                EmptyJsonData,
                ISelfServiceJsonSchemaService.LatestVersionNumber,
                ParsedJsonMetadataResultCode.SuccessNoSchema
            );

        // if json schema has no required fields, we can allow an empty json object
        var jsonSchema = JsonSchema.FromText(latestSchema.Schema);
        var requiredFields = jsonSchema.GetRequired();
        if (requiredFields != null && requiredFields.Any())
        {
            return ParsedJsonMetadataResult.CreateError("Invalid Json Metadata");
        }

        return ParsedJsonMetadataResult.CreateSuccess(
            EmptyJsonData,
            latestSchema.SchemaVersion,
            ParsedJsonMetadataResultCode.SuccessSchemaHasNoRequiredFields
        );
    }
}
