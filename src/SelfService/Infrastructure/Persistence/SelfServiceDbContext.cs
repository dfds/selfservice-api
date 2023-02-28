using Dafda.Outbox;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Converters;

namespace SelfService.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static void AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<SelfServiceDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration["SS_CONNECTION_STRING"]);
        });
    }
}

public class SelfServiceDbContext : DbContext
{
    public SelfServiceDbContext(DbContextOptions<SelfServiceDbContext> options) : base(options)
    {

    }

    public DbSet<OutboxEntry> OutboxEntries { get; set; } = null!;

    public DbSet<Capability> Capabilities { get; set; } = null!;
    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<Membership> Memberships { get; set; } = null!;
    public DbSet<MembershipApplication> MembershipApplications { get; set; } = null!;
    public DbSet<AwsAccount> AwsAccounts { get; set; } = null!;

    public DbSet<KafkaCluster> KafkaClusters { get; set; } = null!;
    public DbSet<KafkaTopic> KafkaTopics { get; set; } = null!;

    public DbSet<ServiceDescription> ServiceCatalog { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<ForceUtc>();

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

        configurationBuilder
            .Properties<KafkaClusterId>()
            .HaveConversion<KafkaClusterIdConverter>();

        configurationBuilder
            .Properties<KafkaTopicId>()
            .HaveConversion<KafkaTopicIdConverter>();

        configurationBuilder
            .Properties<KafkaTopicName>()
            .HaveConversion<KafkaTopicNameConverter>();

        configurationBuilder
            .Properties<KafkaTopicStatusType>()
            .HaveConversion<string>();

        configurationBuilder
            .Properties<MembershipApplicationId>()
            .HaveConversion<MembershipApplicationIdConverter>();

        configurationBuilder
            .Properties<MempershipApplicationStatusOptions>()
            .HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxEntry>(cfg =>
        {
            cfg.ToTable("_outbox");
            cfg.HasKey(x => x.MessageId);
            cfg.Property(x => x.MessageId).HasColumnName("Id");
            cfg.Property(x => x.Topic);
            cfg.Property(x => x.Key);
            cfg.Property(x => x.Payload);
            cfg.Property(x => x.OccurredUtc);
            cfg.Property(x => x.ProcessedUtc);
        });

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

        modelBuilder.Entity<Member>(cfg =>
        {
            cfg.ToTable("Member");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.DisplayName);
            cfg.Property(x => x.Email);
        });

        modelBuilder.Entity<Membership>(opt =>
        {
            opt.ToTable("Membership");
            opt.HasKey(x => x.Id);
            opt.Property(x => x.CapabilityId);
            opt.Property(x => x.UserId);
            opt.Property(x => x.CreatedAt);
        });

        modelBuilder.Entity<MembershipApplication>(opt =>
        {
            opt.ToTable("MembershipApplication");
            opt.HasKey(x => x.Id);
            opt.Property(x => x.CapabilityId);
            opt.Property(x => x.Applicant);
            opt.Property(x => x.Status);
            opt.Property(x => x.SubmittedAt);
            opt.Property(x => x.ExpiresOn);

            opt.HasMany(x => x.Approvals);

            opt.Ignore(x => x.IsFinalized);
            opt.Ignore(x => x.IsCancelled);
        });

        modelBuilder.Entity<MembershipApproval>(cfg =>
        {
            cfg.ToTable("MembershipApproval");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.ApprovedBy);
            cfg.Property(x => x.ApprovedAt);
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

        modelBuilder.Entity<KafkaCluster>(cfg =>
        {
            cfg.ToTable("KafkaCluster");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Name);
            cfg.Property(x => x.Description);
            cfg.Property(x => x.Enabled);
        });

        modelBuilder.Entity<KafkaTopic>(cfg =>
        {
            cfg.ToTable("KafkaTopic");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.KafkaClusterId);
            cfg.Property(x => x.CapabilityId);
            cfg.Property(x => x.Name);
            cfg.Property(x => x.Description);
            cfg.Property(x => x.Status);
            cfg.Property(x => x.Partitions);
            cfg.Property(x => x.Retention);
            cfg.Property(x => x.CreatedAt);
            cfg.Property(x => x.CreatedBy);
            cfg.Property(x => x.ModifiedAt);
            cfg.Property(x => x.ModifiedBy);
        });

        // ----------------------------------------------

        modelBuilder.Entity<ServiceDescription>(cfg =>
        {
            cfg.ToTable("ServiceCatalog");
        });
    }
}