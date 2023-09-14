namespace SelfService.Domain.Models;

public interface ISelfServiceJsonSchemaRepository
{
    Task<SelfServiceJsonSchema> GetSchema(SelfServiceJsonSchemaObjectId objectId, int schemaVersion);
    Task<SelfServiceJsonSchema> GetLatestSchema(SelfServiceJsonSchemaObjectId objectId);
    Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchema selfServiceJsonSchema);
}
