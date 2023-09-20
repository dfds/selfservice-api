using System.Text.Json.Nodes;
using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface ISelfServiceJsonSchemaService
{
    public const int LatestVersionNumber = 0;

    public Task<SelfServiceJsonSchema?> GetSchema(
        SelfServiceJsonSchemaObjectId objectId,
        int schemaVersion = LatestVersionNumber
    );

    public Task<SelfServiceJsonSchema> AddSchema(
        SelfServiceJsonSchemaObjectId objectId,
        string schema,
        int requestedSchemaVersion
    );
    public Task<JsonObject?> GetEmptyJsonDataObjectFromLatestSchema(SelfServiceJsonSchemaObjectId objectId);
    public Task<bool> IsJsonDataValid(string jsonSchemaString, string jsonData);

    public Task<ParsedJsonMetadataResult> GetOrCreateJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata,
        Func<JsonObject, JsonObject>? customGeneratedSchemaJsonObjectModifications = null!
    );
}
