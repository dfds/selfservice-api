using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Legacy;

public static class LegacyConfiguration
{
    public static void AddLegacy(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<LegacyDbContext>(options => {options.UseNpgsql(builder.Configuration["SS_LEGACY_CONNECTION_STRING"]);});

        builder.Services.AddHostedService<Synchronizer>();
    }
}