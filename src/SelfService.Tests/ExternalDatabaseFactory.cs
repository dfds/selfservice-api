using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests;

public class ExternalDatabaseFactory : IDisposable, IAsyncDisposable
{
    private const string DefaultConnectionString =
        "User ID=postgres;Password=p;Host=localhost;Port=5432;Database=db;timeout=2;Command Timeout=2;";

    private DbConnection? _connection;
    private SelfServiceDbContext? _dbContext;

    public async Task<SelfServiceDbContext> CreateDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("SS_CONNECTION_STRING") ?? DefaultConnectionString;
        _connection = new NpgsqlConnection(connectionString!);

        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SelfServiceDbContext>().UseNpgsql(_connection).Options;

        _dbContext = new SelfServiceDbContext(options);
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
