using Microsoft.EntityFrameworkCore;
using SelfService.Legacy.Models;

namespace SelfService.Legacy;

public class LegacyDbContext : DbContext
{
    public LegacyDbContext(DbContextOptions<LegacyDbContext> options) : base(options)
    {
    }

    public DbSet<Capability> Capabilities { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Cluster> Clusters { get; set; }

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

        modelBuilder.Entity<Context>(cfg =>
        {
            cfg.ToTable("Context");
        });

        modelBuilder.Entity<Membership>(cfg =>
        {
            cfg.ToTable("Membership");

            cfg.Property(x => x.Email)
                .HasColumnName("MemberEmail");
        });

        modelBuilder.Entity<Topic>(cfg =>
        {
            cfg.ToTable("KafkaTopic");
            cfg.Ignore(t => t.Configurations);

            // cfg.Property(t => t.Configurations)
            // 	.HasConversion(
            // 		d => JsonConvert.SerializeObject(d, Formatting.None),
            // 		s => JsonConvert.DeserializeObject<Dictionary<string, object>>(s)
            // 	)
            // 	.HasMaxLength(4096);
        });

        modelBuilder.Entity<Cluster>(cfg =>
        {
            cfg.ToTable("KafkaCluster");
        });

    }
}