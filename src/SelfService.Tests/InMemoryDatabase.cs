using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests;

public class InMemoryDatabaseFactory : IDisposable, IAsyncDisposable
{
    private SqliteConnection? _connection;
    private DbContext? _dbContext;

    public Task<SelfServiceDbContext> CreateSelfServiceDbContext(bool initializeSchema = true)
    {
        return CreateDbContext<SelfServiceDbContext>(options => new SelfServiceDbContext(options), initializeSchema);
    }

    public async Task<T> CreateDbContext<T>(
        Func<DbContextOptions<SelfServiceDbContext>, T> constructorFunc,
        bool initializeSchema = true
    )
        where T : DbContext
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SelfServiceDbContext>().UseSqlite(_connection).Options;
        _dbContext = constructorFunc.Invoke(options);

        if (_dbContext is null)
            throw new Exception("Could not create DbContext");

        if (initializeSchema)
        {
            await _dbContext.Database.EnsureCreatedAsync();
        }

        return (T)_dbContext;
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
