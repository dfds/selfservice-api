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

    public Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchemaObjectId objectId, string schema);

    public Task<ParsedJsonMetadataResult> ParseJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata
    );
}
