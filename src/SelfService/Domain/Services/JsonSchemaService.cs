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

    public bool HasJsonSchema(SelfServiceJsonSchemaObjectId objectId)
    {
        try
        {
            _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task<SelfServiceJsonSchema> GetSchema(
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
        var latestVersionNumber = ISelfServiceJsonSchemaService.LatestVersionNumber;
        try
        {
            var latest = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
            latestVersionNumber = latest.SchemaVersion;
        }
        catch (InvalidOperationException)
        {
            // No schema exists yet, ignore the exception and create the first version
        }

        var newSchema = new SelfServiceJsonSchema(latestVersionNumber + 1, objectId, schema);
        _logger.LogInformation("Adding new SelfServiceJsonSchema to the database: {SelfServiceJsonSchema}", newSchema);
        return await _selfServiceJsonSchemaRepository.AddSchema(newSchema);
    }

    public async Task<JsonObject?> GetEmptyJsonDataObjectFromLatestSchema(SelfServiceJsonSchemaObjectId objectId)
    {
        if (!HasJsonSchema(objectId))
        {
            return await Task.FromResult<JsonObject?>(null);
        }

        var latestSchema = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        var jsonSchema = JsonSchema.FromText(latestSchema.Schema);

        var data = jsonSchema.GenerateData();
        return (JsonObject?)data.Result;
    }

    public async Task<bool> IsJsonDataValid(SelfServiceJsonSchemaObjectId objectId, string jsonData)
    {
        var latestSchema = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        var jsonSchema = JsonSchema.FromText(latestSchema.Schema);
        var result = jsonSchema.Evaluate(jsonData);
        return result.IsValid;
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

        if (requestJsonMetadata != null)
        {
            if (!await IsJsonDataValid(objectId, requestJsonMetadata))
                return ParsedJsonMetadataResult.CreateError("Json metadata from request is not valid against schema");

            return ParsedJsonMetadataResult.CreateSuccess(
                requestJsonMetadata,
                ParsedJsonMetadataResultCode.SuccessFromRequest
            );
        }

        if (!HasJsonSchema(objectId))
            return ParsedJsonMetadataResult.CreateSuccess("", ParsedJsonMetadataResultCode.SuccessNoSchema);

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

        if (!await IsJsonDataValid(objectId, constructedJsonString))
        {
            return ParsedJsonMetadataResult.CreateError("Could not construct json metadata from schema");
        }

        return ParsedJsonMetadataResult.CreateSuccess(
            constructedJsonString,
            ParsedJsonMetadataResultCode.SuccessConstructed
        );
    }
}
