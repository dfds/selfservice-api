using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Configuration;

public static class Domain
{
    public static void AddDomain(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(_ => SystemTime.Default);

        builder.Services.AddTransient<ICapabilityApplicationService, CapabilityApplicationService>();
        builder.Services.AddTransient<IMembershipApplicationService, MembershipApplicationService>();
        builder.Services.AddTransient<IKafkaTopicApplicationService, KafkaTopicApplicationService>();

        builder.Services.AddTransient<IAuthorizationService, AuthorizationService>();

        builder.Services.AddTransient<ICapabilityRepository, CapabilityRepository>();
        builder.Services.AddTransient<IAwsAccountRepository, AwsAccountRepository>();
        builder.Services.AddTransient<IMembershipRepository, MembershipRepository>();
        builder.Services.AddTransient<IMembershipApplicationRepository, MembershipApplicationRepository>();
        builder.Services.AddTransient<IKafkaClusterRepository, KafkaClusterRepository>();
        builder.Services.AddTransient<IKafkaTopicRepository, KafkaTopicRepository>();
        builder.Services.AddTransient<IMessageContractRepository, MessageContractRepository>();
        
        builder.Services.AddTransient<IDbTransactionFacade, RealDbTransactionFacade>();
        
        builder.Services.AddTransient<ICapabilityKafkaTopicsQuery, CapabilityKafkaTopicsQuery>();
        builder.Services.AddTransient<ICapabilityMembersQuery, CapabilityMembersQuery>();
        builder.Services.AddTransient<IMyCapabilitiesQuery, MyCapabilitiesQuery>();
        builder.Services.AddTransient<IMembershipQuery, MembershipQuery>();

        // background jobs
        builder.Services.AddHostedService<CancelExpiredMembershipApplications>();
    }
}