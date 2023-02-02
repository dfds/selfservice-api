using Microsoft.EntityFrameworkCore;
using SelfService.Legacy.Models;

namespace SelfService.Legacy;

public class LegacyDbContext : DbContext
{
    public LegacyDbContext(DbContextOptions<LegacyDbContext> options) : base(options)
    {
    }

    public DbSet<Capability> Capabilities { get; set; }
    public DbSet<Cluster> Clusters { get; set; }
    public DbSet<Topic> Topics { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Capability>(cfg =>
        {
            cfg.ToTable("Capability");

            cfg.HasMany<Membership>(x => x.Memberships)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            cfg.HasMany<Context>(x => x.Contexts)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Context>().ToTable("Context");

        modelBuilder.Entity<Membership>(cfg =>
        {
            cfg.ToTable("Membership");

            cfg.Property(x => x.Email).HasColumnName("MemberEmail");
        });

        modelBuilder.Entity<Topic>().ToTable("KafkaTopic");
        modelBuilder.Entity<Cluster>().ToTable("KafkaCluster");
    }
}