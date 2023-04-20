using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;

namespace SelfService.Tests.Infrastructure.Persistence;

public class DatabaseFixture : IAsyncLifetime
{
    private const string HostAlias = "deep-thought";
    private const string Database = "db";
    private const string Username = "postgres";
    private const string Password = "p";

    private INetwork? _network;
    private PostgreSqlContainer? _postgresContainer;
    private IFutureDockerImage? _migrationsImage;
    private IContainer? _migrationsContainer;

    public string? ConnectionString => _postgresContainer?.GetConnectionString();

    public async Task InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithCleanUp(true)
            .Build();

        await _network.CreateAsync();

        _postgresContainer = new PostgreSqlBuilder()
            .WithHostname("database")
            .WithDatabase(Database)
            .WithUsername(Username)
            .WithPassword(Password)
            .WithPortBinding(5432, 5432)
            .WithCleanUp(true)
            .WithNetwork(_network)
            .WithNetworkAliases(HostAlias)
            .Build();

        await _postgresContainer.StartAsync();

        _migrationsImage = new ImageFromDockerfileBuilder()
            .WithName("integration-test-db-migrations")
            .WithDockerfile("Dockerfile")
            .WithDockerfileDirectory("../../../../../db/")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .Build();

        await _migrationsImage.CreateAsync();

        _migrationsContainer = new ContainerBuilder()
            .WithImage(_migrationsImage)
            .WithWaitStrategy(Wait.ForUnixContainer())
            .WithEnvironment("SEED_CSV_SEPARATOR", ";")
            .WithEnvironment("LOCAL_DEVELOPMENT", "1")
            .WithEnvironment("PGDATABASE", Database)
            .WithEnvironment("PGHOST", HostAlias)
            .WithEnvironment("PGUSER", Username)
            .WithEnvironment("PGPASSWORD", Password)
            .WithEnvironment("PGSSLMODE", "disable")
            .WithCleanUp(true)
            .WithNetwork(_network)
            .Build();

        await _migrationsContainer.StartAsync();
        var exitCode = await _migrationsContainer.GetExitCode();
        if (exitCode > 0)
        {
            var (stdout, stderr) = await _migrationsContainer.GetLogsAsync();
            throw new Exception("Database migrations failed\n" + stdout + "\n" + stderr);
        }
    }
    
    public async Task DisposeAsync()
    {
        if (_migrationsContainer != null)
        {
            await _migrationsContainer.DisposeAsync();
        }

        if (_migrationsImage != null)
        {
            await _migrationsImage.DeleteAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }

        if (_network != null)
        {
            await _network.DeleteAsync();
        }
    }
}