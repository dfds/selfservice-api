using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static void AddDatabase(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<LegacyDbContext>(options => { options.UseNpgsql(builder.Configuration["SS_LEGACY_CONNECTION_STRING"]); });

            builder.Services.AddDbContext<SelfServiceDbContext>(options => { options.UseNpgsql(builder.Configuration["SS_CONNECTION_STRING"]); });
        }
    }

    public class SelfServiceDbContext : DbContext
    {
        public SelfServiceDbContext(DbContextOptions<SelfServiceDbContext> options) : base(options)
        {
        }

        public DbSet<ServiceDescription> ServiceCatalog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ServiceDescription>(cfg =>
            {
                cfg.ToTable("ServiceCatalog");
            });
        }
    }
}