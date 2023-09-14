namespace SelfService.Domain.Models;

public interface ISelfServiceJsonSchemaRepository
{
    Task<SelfServiceJsonSchema> GetSchema(int schemaVersion);
    Task<SelfServiceJsonSchema> GetLatestSchema(string objectId);
    Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchema selfServiceJsonSchema);
}
