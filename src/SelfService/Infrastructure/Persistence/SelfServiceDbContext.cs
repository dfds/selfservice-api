﻿using Dafda.Outbox;
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

            if (builder.Environment.IsDevelopment())
            {
                options
                    .UseLoggerFactory(
                        LoggerFactory.Create(loggerConfig =>
                        {
                            loggerConfig
                                .AddConsole()
                                .AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information);
                        })
                    )
                    .EnableSensitiveDataLogging();
            }
        });
    }
}

public class SelfServiceDbContext : DbContext
{
    // Parameterless constructor is required by EntityFramework
    public SelfServiceDbContext(DbContextOptions<SelfServiceDbContext> options)
        : base(options) { }

    public DbSet<OutboxEntry> OutboxEntries { get; set; } = null!; // suppress parameterless constructor warning

    public DbSet<Capability> Capabilities { get; set; } = null!; // suppress parameterless constructor warning
    public DbSet<Member> Members { get; set; } = null!; // suppress parameterless constructor warning
    public DbSet<Membership> Memberships { get; set; } = null!; // suppress parameterless constructor warning
    public DbSet<MembershipApplication> MembershipApplications { get; set; } = null!; // suppress parameterless constructor warning
    public DbSet<AwsAccount> AwsAccounts { get; set; } = null!; // suppress parameterless constructor warning

    public DbSet<KafkaCluster> KafkaClusters { get; set; } = null!; // suppress parameterless constructor warning
    public DbSet<KafkaClusterAccess> KafkaClusterAccess { get; set; } = null!; // suppress parameterless constructor warning
    public DbSet<KafkaTopic> KafkaTopics { get; set; } = null!; // suppress parameterless constructor warning
    public DbSet<MessageContract> MessageContracts { get; set; } = null!; // suppress parameterless constructor warning

    public DbSet<PortalVisit> PortalVisits { get; set; } = null!; // suppress parameterless constructor warning

    public DbSet<ServiceDescription> ServiceCatalog { get; set; } = null!; // suppress parameterless constructor warning

    public DbSet<ECRRepositoryRepository> ECRRepositories { get; set; } = null!; // suppress parameterless constructor warning

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveConversion<ForceUtc>();

        configurationBuilder.Properties<CapabilityId>().HaveConversion<CapabilityIdConverter>();

        configurationBuilder.Properties<UserId>().HaveConversion<UserIdConverter>();

        configurationBuilder.Properties<MembershipId>().HaveConversion<MembershipIdConverter>();

        configurationBuilder.Properties<AwsAccountId>().HaveConversion<AwsAccountIdConverter>();

        configurationBuilder.Properties<RealAwsAccountId>().HaveConversion<RealAwsAccountIdConverter>();

        configurationBuilder.Properties<AwsRoleArn>().HaveConversion<AwsRoleArnConverter>();

        configurationBuilder.Properties<KafkaClusterId>().HaveConversion<KafkaClusterIdConverter>();

        configurationBuilder.Properties<KafkaTopicId>().HaveConversion<KafkaTopicIdConverter>();

        configurationBuilder.Properties<KafkaTopicName>().HaveConversion<KafkaTopicNameConverter>();

        configurationBuilder.Properties<KafkaTopicStatus>().HaveConversion<KafkaTopicStatusConverter>();

        configurationBuilder.Properties<MembershipApplicationId>().HaveConversion<MembershipApplicationIdConverter>();

        configurationBuilder
            .Properties<MembershipApplicationStatusOptions>()
            .HaveConversion<MembershipApplicationStatusOptionsConverter>();

        configurationBuilder.Properties<CapabilityStatusOptions>().HaveConversion<CapabilityStatusOptionsConverter>();

        configurationBuilder.Properties<KafkaTopicPartitions>().HaveConversion<KafkaTopicPartitionsConverter>();

        configurationBuilder.Properties<KafkaTopicRetention>().HaveConversion<KafkaTopicRetentionConverter>();

        configurationBuilder.Properties<MessageContractId>().HaveConversion<MessageContractIdConverter>();

        configurationBuilder.Properties<MessageType>().HaveConversion<MessageTypeConverter>();

        configurationBuilder.Properties<MessageContractExample>().HaveConversion<MessageContractExampleConverter>();

        configurationBuilder.Properties<MessageContractSchema>().HaveConversion<MessageContractSchemaConverter>();

        configurationBuilder.Properties<MessageContractStatus>().HaveConversion<MessageContractStatusConverter>();
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
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.Name);
            cfg.Property(x => x.Description);
            cfg.Property(x => x.Status);
            cfg.Property(x => x.CreatedAt);
            cfg.Property(x => x.ModifiedAt);
            cfg.Property(x => x.CreatedBy);
        });

        modelBuilder.Entity<Member>(cfg =>
        {
            cfg.ToTable("Member");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.DisplayName);
            cfg.Property(x => x.Email);
            cfg.Ignore(x => x.LastSeen);
        });

        modelBuilder.Entity<Membership>(cfg =>
        {
            cfg.ToTable("Membership");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.CapabilityId);
            cfg.Property(x => x.UserId);
            cfg.Property(x => x.CreatedAt);
        });

        modelBuilder.Entity<MembershipApplication>(opt =>
        {
            opt.ToTable("MembershipApplication");
            opt.HasKey(x => x.Id);
            opt.Property(x => x.Id).ValueGeneratedNever();
            opt.Property(x => x.CapabilityId);
            opt.Property(x => x.Applicant);
            opt.Property(x => x.Status);
            opt.Property(x => x.SubmittedAt);
            opt.Property(x => x.ExpiresOn);

            opt.HasMany(x => x.Approvals);
            opt.Navigation(x => x.Approvals).AutoInclude();

            opt.Ignore(x => x.IsFinalized);
            opt.Ignore(x => x.IsCancelled);
        });

        modelBuilder.Entity<MembershipApproval>(cfg =>
        {
            cfg.ToTable("MembershipApproval");

            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.ApprovedBy);
            cfg.Property(x => x.ApprovedAt);
        });

        modelBuilder.Entity<AwsAccount>(cfg =>
        {
            cfg.ToTable("AwsAccount");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.CapabilityId);
            cfg.OwnsOne(
                x => x.Registration,
                o =>
                {
                    o.Property(x => x.AccountId).HasColumnName(nameof(AwsAccountRegistration.AccountId));
                    o.Property(x => x.RoleEmail).HasColumnName(nameof(AwsAccountRegistration.RoleEmail));
                    o.Property(x => x.RegisteredAt).HasColumnName(nameof(AwsAccountRegistration.RegisteredAt));
                }
            );
            cfg.OwnsOne(
                x => x.KubernetesLink,
                o =>
                {
                    o.Property(x => x.Namespace).HasColumnName(nameof(KubernetesLink.Namespace));
                    o.Property(x => x.LinkedAt).HasColumnName(nameof(KubernetesLink.LinkedAt));
                }
            );
            cfg.Property(x => x.RequestedAt);
            cfg.Property(x => x.RequestedBy);
        });

        modelBuilder.Entity<KafkaCluster>(cfg =>
        {
            cfg.ToTable("KafkaCluster");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.Name);
            cfg.Property(x => x.Description);
            cfg.Property(x => x.Enabled);
            cfg.Property(x => x.BootstrapServers);
            cfg.Property(x => x.SchemaRegistryUrl);
        });

        modelBuilder.Entity<KafkaClusterAccess>(cfg =>
        {
            cfg.ToTable("KafkaClusterAccess");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.CapabilityId);
            cfg.Property(x => x.KafkaClusterId);
            cfg.Property(x => x.GrantedAt);
            cfg.Property(x => x.CreatedAt);
            cfg.Property(x => x.CreatedBy);
        });

        modelBuilder.Entity<KafkaTopic>(cfg =>
        {
            cfg.ToTable("KafkaTopic");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
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

            cfg.Ignore(x => x.IsPublic);
            cfg.Ignore(x => x.IsPrivate);
        });

        modelBuilder.Entity<MessageContract>(cfg =>
        {
            cfg.ToTable("MessageContract");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.KafkaTopicId);
            cfg.Property(x => x.MessageType);
            cfg.Property(x => x.Description);
            cfg.Property(x => x.Example);
            cfg.Property(x => x.Schema);
            cfg.Property(x => x.Status);
            cfg.Property(x => x.CreatedAt);
            cfg.Property(x => x.CreatedBy);
            cfg.Property(x => x.ModifiedAt);
            cfg.Property(x => x.ModifiedBy);
        });

        modelBuilder.Entity<PortalVisit>(cfg =>
        {
            cfg.ToTable("PortalVisit");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.VisitedBy);
            cfg.Property(x => x.VisitedAt);
        });

        // ----------------------------------------------

        modelBuilder.Entity<ServiceDescription>(cfg =>
        {
            cfg.ToTable("ServiceCatalog");
        });

        modelBuilder.Entity<ECRRepository>(cfg =>
        {
            cfg.ToTable("ECRRepository");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).ValueGeneratedNever();
            cfg.Property(x => x.Name);
            cfg.Property(x => x.Description);
            cfg.Property(x => x.RepositoryName);
            cfg.Property(x => x.CreatedBy);
        });
    }
}
