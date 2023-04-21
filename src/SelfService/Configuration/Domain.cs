using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Queries;
using SelfService.Infrastructure.Ticketing;

namespace SelfService.Configuration;

public static class Domain
{
    public static void AddDomain(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(_ => SystemTime.Default);

        // application services
        builder.Services.AddTransient<ICapabilityApplicationService, CapabilityApplicationService>();
        builder.Services.AddTransient<IAwsAccountApplicationService, AwsAccountApplicationService>();
        builder.Services.AddTransient<IMembershipApplicationService, MembershipApplicationService>();
        builder.Services.AddTransient<IKafkaTopicApplicationService, KafkaTopicApplicationService>();
        builder.Services.AddTransient<IMemberApplicationService, MemberApplicationService>();
        builder.Services.AddTransient<IPortalVisitApplicationService, PortalVisitApplicationService>();

        // domain services
        builder.Services.AddTransient<MembershipApplicationDomainService>();
        builder.Services.AddTransient<IAuthorizationService, AuthorizationService>();

        // domain repositories
        builder.Services.AddTransient<ICapabilityRepository, CapabilityRepository>();
        builder.Services.AddTransient<IAwsAccountRepository, AwsAccountRepository>();
        builder.Services.AddTransient<IMembershipRepository, MembershipRepository>();
        builder.Services.AddTransient<IMembershipApplicationRepository, MembershipApplicationRepository>();
        builder.Services.AddTransient<IKafkaClusterRepository, KafkaClusterRepository>();
        builder.Services.AddTransient<IKafkaTopicRepository, KafkaTopicRepository>();
        builder.Services.AddTransient<IMessageContractRepository, MessageContractRepository>();
        builder.Services.AddTransient<IMemberRepository, MemberRepository>();
        builder.Services.AddTransient<IPortalVisitRepository, PortalVisitRepository>();

        builder.Services.AddTransient<TopVisitorsRepository>();

        // domain queries
        builder.Services.AddTransient<ICapabilityKafkaTopicsQuery, CapabilityKafkaTopicsQuery>();
        builder.Services.AddTransient<ICapabilityMembersQuery, CapabilityMembersQuery>();
        builder.Services.AddTransient<IMyCapabilitiesQuery, MyCapabilitiesQuery>();
        builder.Services.AddTransient<IMembershipQuery, MembershipQuery>();
        builder.Services.AddTransient<ICapabilityMembershipApplicationQuery, CapabilityMembershipApplicationQuery>();

        // background jobs
        builder.Services.AddHostedService<CancelExpiredMembershipApplications>();
        builder.Services.AddHostedService<PortalVisitAnalyzer>();

        // misc
        builder.Services.AddTransient<IDbTransactionFacade, RealDbTransactionFacade>();

        var endpoint = new Uri(builder.Configuration["SS_TOPDESK_API_GATEWAY_ENDPOINT"] ?? "");
        var apiKey = builder.Configuration["SS_TOPDESK_API_GATEWAY_API_KEY"];
        builder.Services.AddHttpClient<ITicketingSystem, TopDesk>(client =>
        {
            client.BaseAddress = endpoint;
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        });
    }
}
