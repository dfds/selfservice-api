using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Converters;

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

        public DbSet<Capability> Capabilities { get; set; } = null!;
        public DbSet<Member> Members { get; set; } = null!;
        public DbSet<Membership> Memberships { get; set; } = null!;
        public DbSet<AwsAccount> AwsAccounts { get; set; } = null!;

        public DbSet<KafkaCluster> KafkaClusters { get; set; } = null!;
        public DbSet<KafkaTopic> KafkaTopics { get; set; } = null!;

        public DbSet<ServiceDescription> ServiceCatalog { get; set; } = null!;

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder
                .Properties<CapabilityId>()
                .HaveConversion<CapabilityIdConverter>();

            configurationBuilder
                .Properties<UserId>()
                .HaveConversion<UserIdConverter>();

            configurationBuilder
                .Properties<MembershipId>()
                .HaveConversion<MembershipIdConverter>();

            configurationBuilder
                .Properties<AwsAccountId>()
                .HaveConversion<AwsAccountIdConverter>();

            configurationBuilder
                .Properties<RealAwsAccountId>()
                .HaveConversion<RealAwsAccountIdConverter>();

            configurationBuilder
                .Properties<AwsRoleArn>()
                .HaveConversion<AwsRoleArnConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Capability>(cfg =>
            {
                cfg.ToTable("Capability");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.Name);
                cfg.Property(x => x.Description);
                cfg.Property(x => x.Deleted);
                cfg.Property(x => x.CreatedAt);
                cfg.Property(x => x.CreatedBy);
            });

            modelBuilder.Entity<Membership>(opt =>
            {
                opt.ToTable("Membership");
                opt.HasKey(x => x.Id);
                opt.Property(x => x.CapabilityId);
                opt.Property(x => x.UserId);
                opt.Property(x => x.CreatedAt);
            });

            modelBuilder.Entity<Member>(cfg =>
            {
                cfg.ToTable("Member");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.DisplayName);
                cfg.Property(x => x.Email);
            });

            modelBuilder.Entity<AwsAccount>(cfg =>
            {
                cfg.ToTable("AwsAccount");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.CapabilityId);
                cfg.Property(x => x.AccountId);
                cfg.Property(x => x.RoleArn);
                cfg.Property(x => x.RoleEmail);
                cfg.Property(x => x.CreatedAt);
                cfg.Property(x => x.CreatedBy);
            });

            // ----------------------------------------------

            modelBuilder.Entity<KafkaCluster>(cfg =>
            {
                cfg.ToTable("KafkaCluster");
            });

            modelBuilder.Entity<KafkaTopic>(cfg =>
            {
                cfg.ToTable("KafkaTopic");
            });

            modelBuilder.Entity<ServiceDescription>(cfg =>
            {
                cfg.ToTable("ServiceCatalog");
            });
        }
    }
}