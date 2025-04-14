using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.BackgroundJobs;

public class CheckMembershipDiscrepancies : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public CheckMembershipDiscrepancies(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await DoWork(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(24), stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CheckMembershipDiscrepancies>>();

        using var _ = logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(CheckMembershipDiscrepancies),
            Guid.NewGuid()
        );
        var membershipRepository = scope.ServiceProvider.GetRequiredService<IMembershipRepository>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IMembershipApplicationRepository>();
        var applicationService = scope.ServiceProvider.GetRequiredService<IMembershipApplicationService>();
        var invitationRepository = scope.ServiceProvider.GetRequiredService<IInvitationRepository>();
        var invitationService = scope.ServiceProvider.GetRequiredService<IInvitationApplicationService>();

        logger.LogDebug("Checking for invitation and membership discrepancies...");
        // find all outstanding invitations and check if a membership already exists
        var invitations = await invitationRepository.GetAllWithPredicate(x =>
            x.Status == InvitationStatusOptions.Active
        );
        foreach (var invitation in invitations)
        {
            var relevantMemberships = await membershipRepository.GetAllWithPredicate(x =>
                x.UserId == invitation.Invitee && x.CapabilityId == invitation.TargetId
            );
            if (relevantMemberships.Count > 0)
            {
                await invitationService.DeclineInvitation(invitation.Id);
                logger.LogInformation(
                    "Invitation {InvitationId} for user {UserId} to capability {CapabilityId} was already satisfied",
                    invitation.Id,
                    invitation.Invitee,
                    invitation.TargetId
                );
            }
        }

        logger.LogDebug("Checking for invitation and membership discrepancies...");
        var applications = await applicationRepository.FindAllPending();
        foreach (var application in applications)
        {
            var relevantMemberships = await membershipRepository.GetAllWithPredicate(x =>
                x.UserId == application.Applicant && x.CapabilityId == application.CapabilityId
            );
            if (relevantMemberships.Count > 0)
            {
                await applicationService.RemoveMembershipApplication(application.Id);
                logger.LogInformation(
                    "Application {ApplicationId} for user {UserId} to capability {CapabilityId} was already satisfied",
                    application.Id,
                    application.Applicant,
                    application.CapabilityId
                );
            }
        }
    }
}
