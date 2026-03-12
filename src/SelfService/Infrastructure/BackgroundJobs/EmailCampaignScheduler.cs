using Cronos;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.BackgroundJobs;

public class EmailCampaignScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public EmailCampaignScheduler(IServiceProvider serviceProvider)
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
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            },
            stoppingToken
        );
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EmailCampaignScheduler>>();

        using var _ = logger.BeginScope(
            "{BackgroundJob} {CorrelationId}",
            nameof(EmailCampaignScheduler),
            Guid.NewGuid()
        );

        var applicationService =
            scope.ServiceProvider.GetRequiredService<IEmailCampaignApplicationService>();
        var campaignRepository =
            scope.ServiceProvider.GetRequiredService<IEmailCampaignRepository>();
        var executionRepository =
            scope.ServiceProvider.GetRequiredService<IEmailCampaignExecutionRepository>();

        try
        {
            // Process one-time scheduled campaigns that are due
            var dueScheduled = await campaignRepository.GetDueScheduled();
            foreach (var campaign in dueScheduled)
            {
                try
                {
                    logger.LogInformation(
                        "Executing scheduled campaign {CampaignId} ({Name})",
                        campaign.Id,
                        campaign.Name
                    );

                    // Phase 1: Transition to Sending (committed immediately via [TransactionalBoundary])
                    await applicationService.MarkCampaignAsSending(campaign.Id);

                    // Phase 2: Execute send (if this fails, campaign stays Sending, not re-queued)
                    await applicationService.ExecuteScheduledCampaign(campaign.Id);
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e,
                        "Error executing scheduled campaign {CampaignId}",
                        campaign.Id
                    );
                }
            }

            // Process recurring campaigns that are due
            var dueRecurring = await campaignRepository.GetDueRecurring();
            foreach (var campaign in dueRecurring)
            {
                try
                {
                    if (!await IsCronDue(campaign, executionRepository, logger))
                        continue;

                    logger.LogInformation(
                        "Executing recurring campaign {CampaignId} ({Name}), cron: {Cron}",
                        campaign.Id,
                        campaign.Name,
                        campaign.CronExpression
                    );
                    await applicationService.ExecuteRecurringCampaign(campaign.Id);
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e,
                        "Error executing recurring campaign {CampaignId}",
                        campaign.Id
                    );
                }
            }

            // Warn about campaigns stuck in Sending status
            var sendingCampaigns = await campaignRepository.GetByStatus(EmailCampaignStatus.Sending);
            foreach (var stuck in sendingCampaigns)
            {
                var stuckDuration = DateTime.UtcNow - stuck.ModifiedAt;
                if (stuckDuration > TimeSpan.FromMinutes(10))
                {
                    logger.LogWarning(
                        "Campaign {CampaignId} ({Name}) has been stuck in Sending status for {Duration}",
                        stuck.Id,
                        stuck.Name,
                        stuckDuration
                    );
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in email campaign scheduler");
        }
    }

    private static async Task<bool> IsCronDue(
        EmailCampaign campaign,
        IEmailCampaignExecutionRepository executionRepository,
        ILogger logger
    )
    {
        if (string.IsNullOrEmpty(campaign.CronExpression))
            return false;

        try
        {
            var format = campaign.CronExpression.Split(' ').Length == 6
                ? CronFormat.IncludeSeconds
                : CronFormat.Standard;
            var cronExpression = CronExpression.Parse(campaign.CronExpression, format);
            var lastExecution = await executionRepository.GetLatestByCampaignId(campaign.Id);
            var referenceTime = lastExecution?.ExecutedAt ?? campaign.ScheduledAt ?? campaign.CreatedAt;
            var referenceTimeUtc = DateTime.SpecifyKind(referenceTime, DateTimeKind.Utc);
            var nextOccurrence = cronExpression.GetNextOccurrence(referenceTimeUtc, TimeZoneInfo.Utc);

            return nextOccurrence.HasValue && nextOccurrence.Value <= DateTime.UtcNow;
        }
        catch (CronFormatException e)
        {
            logger.LogWarning(
                e,
                "Invalid cron expression for campaign {CampaignId}: {Cron}",
                campaign.Id,
                campaign.CronExpression
            );
            return false;
        }
    }
}
