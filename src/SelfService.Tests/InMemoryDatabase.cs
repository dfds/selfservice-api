using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests;

public class InMemoryDatabaseFactory : IDisposable, IAsyncDisposable
{
    private SqliteConnection? _connection;
    private SelfServiceDbContext? _dbContext;

    public async Task<SelfServiceDbContext> CreateDbContext(bool initializeSchema = true)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SelfServiceDbContext>().UseSqlite(_connection).Options;
        _dbContext = new SelfServiceDbContext(options);

        if (initializeSchema)
        {
            await _dbContext.Database.EnsureCreatedAsync();
        }

        return _dbContext;
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