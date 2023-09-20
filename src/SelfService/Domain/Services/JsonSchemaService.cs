using System.Text.Json;
using System.Text.Json.Nodes;
using SelfService.Domain.Models;
using Json.Schema;
using Json.Schema.DataGeneration;

namespace SelfService.Domain.Services;

public class SelfServiceJsonSchemaService : ISelfServiceJsonSchemaService
{
    private readonly ILogger<ECRRepositoryService> _logger;
    private readonly ISelfServiceJsonSchemaRepository _selfServiceJsonSchemaRepository;

    public SelfServiceJsonSchemaService(
        ILogger<ECRRepositoryService> logger,
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
    public async Task<SelfServiceJsonSchema> AddSchema(
        SelfServiceJsonSchemaObjectId objectId,
        string schema,
        int requestedSchemaVersion
    )
    {
        var latest = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        var latestVersionNumber = latest?.SchemaVersion ?? ISelfServiceJsonSchemaService.LatestVersionNumber;

        var targetVersion = latestVersionNumber + 1;
        if (targetVersion > requestedSchemaVersion)
        {
            // TODO: Create custom exception type
            throw new Exception(
                $"Requested schema version {requestedSchemaVersion} is lower than latest schema version {targetVersion}, fetch latest changes and try again"
            );
        }

        if (targetVersion < requestedSchemaVersion)
        {
            throw new Exception(
                $"Requested schema version {requestedSchemaVersion} is higher than latest schema version {targetVersion}, fetch latest changes and try again"
            );
        }

        var newSchema = new SelfServiceJsonSchema(latestVersionNumber + 1, objectId, schema);
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

    private JsonObject EmptyCustomModification(JsonObject jsonObject)
    {
        return jsonObject;
    }

    public async Task<ParsedJsonMetadataResult> GetOrCreateJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata,
        Func<JsonObject, JsonObject>? customGeneratedSchemaJsonObjectModifications = null
    )
    {
        customGeneratedSchemaJsonObjectModifications ??= EmptyCustomModification;
        var latestSchema = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        if (requestJsonMetadata != null)
        {
            if (latestSchema == null)
                return ParsedJsonMetadataResult.CreateError("Json metadata from request is not valid against schema");

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
                "",
                ISelfServiceJsonSchemaService.LatestVersionNumber,
                ParsedJsonMetadataResultCode.SuccessNoSchema
            );

        _logger.LogInformation(
            "Attempting to construct JsonMetadata from JsonSchema for {JsonSchemaObjectId}",
            objectId
        );
        // try constructing metadata from object schema
        var jsonObject = await GetEmptyJsonDataObjectFromLatestSchema(objectId);
        if (jsonObject == null)
        {
            return ParsedJsonMetadataResult.CreateError("Could not construct json metadata from schema");
        }

        // Modify the json object according to input modifications
        // This is relevant in cases where SelfService API have an idea of how the schema looks like
        jsonObject = customGeneratedSchemaJsonObjectModifications(jsonObject);

        var constructedJsonString = jsonObject.ToJsonString();

        if (!await IsJsonDataValid(latestSchema.Schema, constructedJsonString))
        {
            return ParsedJsonMetadataResult.CreateError("Could not construct json metadata from schema");
        }

        return ParsedJsonMetadataResult.CreateSuccess(
            constructedJsonString,
            latestSchema.SchemaVersion,
            ParsedJsonMetadataResultCode.SuccessConstructed
        );
    }
}
