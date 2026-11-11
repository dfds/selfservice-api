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

    public Task<SelfServiceJsonSchema?> GetSchema(SelfServiceJsonSchemaObjectId objectId, int schemaVersion)
    {
        return _dbContext.SelfServiceJsonSchemas.SingleOrDefaultAsync(x =>
            x.SchemaVersion == schemaVersion && x.ObjectId == objectId
        );
    }

    public async Task<SelfServiceJsonSchema?> GetLatestSchema(SelfServiceJsonSchemaObjectId objectId)
    {
        if (!await _dbContext.SelfServiceJsonSchemas.AnyAsync())
            return null;
        var latestVersion = await _dbContext.SelfServiceJsonSchemas.MaxAsync(x => x.SchemaVersion);
        return await GetSchema(objectId, latestVersion);
    }

    public async Task<SelfServiceJsonSchema> AddSchema(SelfServiceJsonSchema selfServiceJsonSchema)
    {
        await _dbContext.SelfServiceJsonSchemas.AddAsync(selfServiceJsonSchema);
        return selfServiceJsonSchema;
    }
}
