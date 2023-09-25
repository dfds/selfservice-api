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

    public async Task<ValidateJsonMetadataResult> ValidateJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata
    )
    {
        var latestSchema = await _selfServiceJsonSchemaRepository.GetLatestSchema(objectId);
        if (!string.IsNullOrEmpty(requestJsonMetadata) && requestJsonMetadata != EmptyJsonData)
        {
            if (latestSchema == null)
                return ValidateJsonMetadataResult.CreateError(
                    "Json metadata from request is not empty, but no schema exists"
                );

            var parsedJsonSchema = JsonSchema.FromText(latestSchema.Schema);
            JsonNode? actualObj = JsonNode.Parse(requestJsonMetadata);
            var result = parsedJsonSchema.Evaluate(actualObj);
            if (!result.IsValid)
                return ValidateJsonMetadataResult.CreateError(
                    $"Json metadata from request is not valid against schema: {result.Details}"
                );

            return ValidateJsonMetadataResult.CreateSuccess(
                requestJsonMetadata,
                latestSchema.SchemaVersion,
                ValidateJsonMetadataResultCode.SuccessValidJsonMetadata
            );
        }

        if (latestSchema == null)
            return ValidateJsonMetadataResult.CreateSuccess(
                EmptyJsonData,
                ISelfServiceJsonSchemaService.LatestVersionNumber,
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
