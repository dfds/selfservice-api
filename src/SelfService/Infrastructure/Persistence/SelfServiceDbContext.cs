using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static void AddDatabase(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<SelfServiceDbContext>(options => {options.UseNpgsql(builder.Configuration["SS_CONNECTION_STRING"]);});
        }
    }

    public class SelfServiceDbContext : DbContext
    {
        public SelfServiceDbContext(DbContextOptions<SelfServiceDbContext> options) : base(options)
        {
        }

        public DbSet<Capability> Capabilities { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<AwsAccount> AwsAccounts { get; set; }

        public DbSet<KafkaCluster> KafkaClusters { get; set; }
        public DbSet<KafkaTopic> KafkaTopics { get; set; }
        
        public DbSet<ServiceDescription> ServiceCatalog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Capability>().ToTable("Capability").HasOne(x => x.AwsAccount);
            modelBuilder.Entity<Member>().ToTable("Member").HasKey(x => x.UPN);

            modelBuilder.Entity<Membership>(opt =>
            {
                opt.ToTable("Membership");
                
                opt.HasKey(x => new { x.CapabilityId, x.UPN });

                opt.HasOne(x => x.Capability)
                    .WithMany(x => x.Memberships)
                    .HasForeignKey(x => x.CapabilityId);

                opt.HasOne(x => x.Member)
                    .WithMany(x => x.Memberships)
                    .HasForeignKey(x => x.UPN);
            });

            modelBuilder.Entity<AwsAccount>().ToTable("AwsAccount");

            modelBuilder.Entity<KafkaCluster>().ToTable("KafkaCluster");
            modelBuilder.Entity<KafkaTopic>().ToTable("KafkaTopic");

            modelBuilder.Entity<ServiceDescription>().ToTable("ServiceCatalog");
        }
    }
}