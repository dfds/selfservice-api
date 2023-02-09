using Microsoft.EntityFrameworkCore;

namespace SelfService.Legacy;

public static class LegacyConfiguration
{
    public static void AddLegacy(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<LegacyDbContext>(options => {options.UseNpgsql(builder.Configuration["SS_LEGACY_CONNECTION_STRING"]);});

        if (!int.TryParse(builder.Configuration["SS_SYNC_TIMEOUT_SECONDS"], out var syncTimeoutSeconds) || syncTimeoutSeconds <= 0)
        {
            return;
        }

        builder.Services.AddHostedService<Synchronizer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Synchronizer>>();
            var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            return new Synchronizer(logger, serviceScopeFactory, syncTimeoutSeconds);
        });

        builder.Services.AddTransient<CapabilitySynchronizer>();
        builder.Services.AddTransient<KafkaSynchronizer>();
    }
}