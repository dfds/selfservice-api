using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class SelfServiceJsonSchemaRepository : ISelfServiceJsonSchemaRepository
{
    private SelfServiceDbContext _dbContext;

    public SelfServiceJsonSchemaRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SelfServiceJsonSchema> GetSchema(SelfServiceJsonSchemaObjectId objectId, int schemaVersion)
    {
        return _dbContext.SelfServiceJsonSchemas.SingleAsync(
            x => x.SchemaVersion == schemaVersion && x.ObjectId == objectId
        );
    }

    public Task<SelfServiceJsonSchema> GetLatestSchema(SelfServiceJsonSchemaObjectId objectId)
    {
        var latestVersion = _dbContext.SelfServiceJsonSchemas.Max(x => x.SchemaVersion);
        return GetSchema(objectId, latestVersion);
    }

    public Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchema selfServiceJsonSchema)
    {
        _dbContext.SelfServiceJsonSchemas.AddAsync(selfServiceJsonSchema);
        return Task.FromResult(selfServiceJsonSchema);
    }
}
