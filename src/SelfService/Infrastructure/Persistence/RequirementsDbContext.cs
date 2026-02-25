using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SelfService.Application;
using SelfService.Infrastructure.Persistence.Models;

namespace SelfService.Infrastructure.Persistence;

public static class RequirementsDependencyInjection
{
    public static void AddRequirementsDatabase(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration["SS_REQUIREMENTS_CONNECTION_STRING"];

        // Only add the requirements database if a connection string is configured
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Register stub service when database is not configured
            builder.Services.AddTransient<IRequirementsMetricService, StubRequirementsMetricService>();
            return;
        }

        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        builder.Services.AddDbContext<RequirementsDbContext>(options =>
        {
            options.UseNpgsql(dataSource);

            if (builder.Environment.IsDevelopment())
            {
                options
                    .UseLoggerFactory(
                        LoggerFactory.Create(loggerConfig =>
                        {
                            loggerConfig
                                .AddConsole()
                                .AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Warning);
                        })
                    )
                    .EnableSensitiveDataLogging();
            }
        });

        // Register real service when database is configured
        builder.Services.AddTransient<IRequirementsMetricService, RequirementsMetricService>();
    }
}

public class RequirementsDbContext : DbContext
{
    // Parameterless constructor is required by EntityFramework
    public RequirementsDbContext(DbContextOptions<RequirementsDbContext> options)
        : base(options) { }

    public DbSet<RequirementsMetric> Metrics => Set<RequirementsMetric>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // Add custom conversions if needed
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RequirementsMetric>(cfg =>
        {
            cfg.ToTable("metrics");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedOnAdd();
            cfg.Property(x => x.Name).IsRequired();
            cfg.Property(x => x.CapabilityRootId).IsRequired();
            cfg.Property(x => x.RequirementId).IsRequired();
            cfg.Property(x => x.Measurement).IsRequired();
            cfg.Property(x => x.HelpUrl);
            cfg.Property(x => x.Owner);
            cfg.Property(x => x.Description);
            cfg.Property(x => x.ClusterName);
            cfg.Property(x => x.Value);
            cfg.Property(x => x.Help);
            cfg.Property(x => x.Type);
            cfg.Property(x => x.Date);
            cfg.Property(x => x.UpdatedAt);
            cfg.Property(x => x.Labels).HasColumnType("jsonb");
        });
        // Add additional entity configurations as needed
    }
}
