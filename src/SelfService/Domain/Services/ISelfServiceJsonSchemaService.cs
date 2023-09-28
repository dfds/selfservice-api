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

    public void MustValidateJsonSchemaAgainstMetaSchema(string schema);

    public Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchemaObjectId objectId, string schema);

    public Task<ValidateJsonMetadataResult> ValidateJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata
    );
}
