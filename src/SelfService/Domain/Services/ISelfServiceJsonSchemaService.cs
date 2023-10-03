using System.Text.Json.Nodes;
using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface ISelfServiceJsonSchemaService
{
    public Task<SelfServiceJsonSchema?> GetLatestSchema(SelfServiceJsonSchemaObjectId objectId);

    public Task<SelfServiceJsonSchema?> GetSchema(SelfServiceJsonSchemaObjectId objectId, int schemaVersion);

    public void MustValidateJsonSchemaAgainstMetaSchema(string schema);

    public Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchemaObjectId objectId, string schema);

    public Task<ValidateJsonMetadataResult> ValidateJsonMetadata(
        SelfServiceJsonSchemaObjectId objectId,
        string? requestJsonMetadata
    );
}
