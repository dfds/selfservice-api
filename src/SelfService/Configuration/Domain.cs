using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.System;
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Queries;
using SelfService.Infrastructure.Ticketing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SelfService.Infrastructure.Api.Prometheus;

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
        builder.Services.AddTransient<IAwsECRRepositoryApplicationService, AwsEcrRepositoryApplicationService>();
        builder.Services.AddTransient<ITeamApplicationService, TeamApplicationService>();
        builder.Services.AddTransient<IInvitationApplicationService, InvitationApplicationService>();

        // domain services
        builder.Services.AddTransient<IMembershipApplicationDomainService, MembershipApplicationDomainService>();
        builder.Services.AddTransient<IAuthorizationService, AuthorizationService>();
        builder.Services.AddTransient<IECRRepositoryService, ECRRepositoryService>();
        builder.Services.AddTransient<ISelfServiceJsonSchemaService, SelfServiceJsonSchemaService>();

        // domain repositories
        builder.Services.AddTransient<ICapabilityRepository, CapabilityRepository>();
        builder.Services.AddTransient<IAwsAccountRepository, AwsAccountRepository>();
        builder.Services.AddTransient<IMembershipRepository, MembershipRepository>();
        builder.Services.AddTransient<IMembershipApplicationRepository, MembershipApplicationRepository>();
        builder.Services.AddTransient<IKafkaClusterRepository, KafkaClusterRepository>();
        builder.Services.AddTransient<IKafkaClusterAccessRepository, KafkaClusterAccessRepository>();
        builder.Services.AddTransient<IKafkaTopicRepository, KafkaTopicRepository>();
        builder.Services.AddTransient<IMessageContractRepository, MessageContractRepository>();
        builder.Services.AddTransient<IMemberRepository, MemberRepository>();
        builder.Services.AddTransient<IPortalVisitRepository, PortalVisitRepository>();
        builder.Services.AddTransient<IECRRepositoryRepository, ECRRepositoryRepository>();
        builder.Services.AddTransient<ISelfServiceJsonSchemaRepository, SelfServiceJsonSchemaRepository>();
        builder.Services.AddTransient<ITeamRepository, TeamRepository>();
        builder.Services.AddTransient<ITeamCapabilityLinkingRepository, TeamCapabilityLinkingRepository>();
        builder.Services.AddTransient<TopVisitorsRepository>();
        builder.Services.AddTransient<IInvitationRepository, InvitationRepository>();

        // domain queries
        builder.Services.AddTransient<IKafkaTopicQuery, KafkaTopicQuery>();
        builder.Services.AddTransient<ICapabilityKafkaTopicsQuery, CapabilityKafkaTopicsQuery>();
        builder.Services.AddTransient<ICapabilityMembersQuery, CapabilityMembersQuery>();
        builder.Services.AddTransient<IMyCapabilitiesQuery, MyCapabilitiesQuery>();
        builder.Services.AddTransient<IMembershipApplicationQuery, MembershipApplicationQuery>();

        builder.Services.AddTransient<IMembershipQuery, MembershipQuery>();
        //builder.Services.AddTransient<MembershipQuery>();
        //builder.Services.AddScoped<IMembershipQuery, CachedMembershipQueryDecorator>(provider =>
        //{
        //    var inner = provider.GetRequiredService<MembershipQuery>();
        //    return new CachedMembershipQueryDecorator(inner);
        //});

        builder.Services.AddTransient<ICapabilityMembershipApplicationQuery, CapabilityMembershipApplicationQuery>();

        // aad-aws-sync
        builder.Services.AddTransient<IAadAwsSyncCapabilityQuery, AadAwsSyncCapabilityQuery>();

        // background jobs
        builder.Services.AddHostedService<CancelExpiredMembershipApplications>();
        builder.Services.AddHostedService<RemoveDeactivatedMemberships>();
        builder.Services.AddHostedService<PortalVisitAnalyzer>();
        builder.Services.AddHostedService<ActOnPendingCapabilityDeletions>();
        builder.Services.AddHostedService<ECRRepositorySynchronizer>();

        // misc
        builder.Services.AddTransient<IDbTransactionFacade, RealDbTransactionFacade>();
        builder.Services.AddTransient<DeactivatedMemberCleanerApplicationService>();

        var topdeskEndpoint = new Uri(builder.Configuration["SS_TOPDESK_API_GATEWAY_ENDPOINT"] ?? "");
        var topdeskApiKey = builder.Configuration["SS_TOPDESK_API_GATEWAY_API_KEY"];
        builder.Services.AddHttpClient<ITicketingSystem, TopDesk>(client =>
        {
            client.BaseAddress = topdeskEndpoint;
            client.DefaultRequestHeaders.Add("x-api-key", topdeskApiKey);
        });

        var prometheusEndpoint = new Uri(builder.Configuration["SS_PROMETHEUS_API_ENDPOINT"] ?? "");
        builder.Services.AddHttpClient<IKafkaTopicConsumerService, PrometheusClient>(client =>
        {
            client.BaseAddress = prometheusEndpoint;
        });

        var platformDataEndpoint = new Uri(builder.Configuration["SS_PLATFORM_DATA_ENDPOINT"] ?? "");
        var dataApiKey = builder.Configuration["SS_PLATFORM_DATA_API_KEY"];
        builder.Services.AddHttpClient<IPlatformDataApiRequesterService, PlatformDataApiRequesterService>(client =>
        {
            client.BaseAddress = platformDataEndpoint;
            client.DefaultRequestHeaders.Add("x-api-key", dataApiKey);
        });
    }
}
