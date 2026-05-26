using Microsoft.Extensions.DependencyInjection;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;

namespace SelfService.Application;

public class EmailCampaignApplicationService : IEmailCampaignApplicationService
{
    private readonly IEmailCampaignRepository _emailCampaignRepository;
    private readonly IEmailCampaignRecipientLogRepository _recipientLogRepository;
    private readonly IEmailCampaignExecutionRepository _executionRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICapabilityFilterService _capabilityFilterService;
    private readonly ITemplateRenderingService _templateRenderingService;
    private readonly IRbacApplicationService _rbacApplicationService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private static readonly List<TemplateVariable> _templateVariables = new()
    {
        new() { Name = "Capability.Id",               Description = "Capability ID slug",           Entity = "Capability", Example = "my-capability-abc12" },
        new() { Name = "Capability.Name",              Description = "Display name",                 Entity = "Capability", Example = "My Capability" },
        new() { Name = "Capability.Description",       Description = "Description text",             Entity = "Capability", Example = "A description..." },
        new() { Name = "Capability.Status",            Description = "Active or Pending Deletion",   Entity = "Capability", Example = "Active" },
        new() { Name = "Capability.CreatedAt",         Description = "Creation date (yyyy-MM-dd)",   Entity = "Capability", Example = "2024-01-15" },
        new() { Name = "Capability.CreatedBy",         Description = "Creator user ID",              Entity = "Capability", Example = "user@dfds.com" },
        new() { Name = "Capability.RequirementScore",  Description = "Compliance score (0-100)",     Entity = "Capability", Example = "85" },
        new() { Name = "Capability.MemberCount",       Description = "Number of members",            Entity = "Capability", Example = "12" },
        new() { Name = "Member.DisplayName",           Description = "Recipient display name",       Entity = "Member",     Example = "Jane Doe" },
        new() { Name = "Member.Email",                 Description = "Recipient email address",      Entity = "Member",     Example = "jane.doe@dfds.com" },
        new() { Name = "Campaign.Name",                Description = "Campaign name",                Entity = "Campaign",   Example = "Q1 Migration Notice" },
        new() { Name = "Date.Today",                   Description = "Current date (yyyy-MM-dd)",    Entity = "Date",       Example = "2024-01-15" },
        new() { Name = "Date.Year",                    Description = "Current year",                 Entity = "Date",       Example = "2024" },
        // Individual requirement scores (dynamic)
        new() { Name = "Requirement.<id>",             Description = "Individual requirement score (0-100). Replace <id> with: mandatory_tags, external_secrets, irsa, k8s-probes, ecr-pull", Entity = "Requirement", Example = "85" },
        new() { Name = "Requirement.<id>.DisplayName", Description = "Requirement display name",     Entity = "Requirement", Example = "Use of Mandatory Tags" },
        new() { Name = "Requirement.<id>.HelpUrl",     Description = "Requirement help URL",         Entity = "Requirement", Example = "https://wiki.dfds.cloud/..." },
        // AWS account
        new() { Name = "Aws.AccountId",                Description = "AWS account number (12-digit)", Entity = "AwsAccount", Example = "123456789012" },
        new() { Name = "Aws.Status",                   Description = "AWS account status",            Entity = "AwsAccount", Example = "Completed" },
        new() { Name = "Aws.Namespace",                Description = "Kubernetes namespace linked to AWS account", Entity = "AwsAccount", Example = "my-capability-abc12" },
        new() { Name = "Aws.RoleEmail",                Description = "AWS account role email",        Entity = "AwsAccount", Example = "aws.123456789012@dfds.com" },
        // Azure resources
        new() { Name = "Azure.ResourceCount",          Description = "Number of Azure resource groups", Entity = "AzureResource", Example = "2" },
        new() { Name = "Azure.Environments",           Description = "Comma-separated Azure environments", Entity = "AzureResource", Example = "dev, prod" },
        new() { Name = "Azure.<env>.Id",               Description = "Resource group ID for a specific environment (e.g. Azure.dev.Id)", Entity = "AzureResource", Example = "a1b2c3d4-e5f6-..." },
        // Membership applications
        new() { Name = "MembershipApplications.PendingCount", Description = "Number of pending membership applications", Entity = "MembershipApplication", Example = "3" },
    };

    public EmailCampaignApplicationService(
        IEmailCampaignRepository emailCampaignRepository,
        IEmailCampaignRecipientLogRepository recipientLogRepository,
        IEmailCampaignExecutionRepository executionRepository,
        ICapabilityRepository capabilityRepository,
        IMembershipRepository membershipRepository,
        IMemberRepository memberRepository,
        ICapabilityFilterService capabilityFilterService,
        ITemplateRenderingService templateRenderingService,
        IRbacApplicationService rbacApplicationService,
        IServiceScopeFactory serviceScopeFactory
    )
    {
        _emailCampaignRepository = emailCampaignRepository;
        _recipientLogRepository = recipientLogRepository;
        _executionRepository = executionRepository;
        _capabilityRepository = capabilityRepository;
        _membershipRepository = membershipRepository;
        _memberRepository = memberRepository;
        _capabilityFilterService = capabilityFilterService;
        _templateRenderingService = templateRenderingService;
        _rbacApplicationService = rbacApplicationService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<EmailCampaign> CreateDraft(
        string name,
        string subject,
        string contentJson,
        string? contentHtml,
        string audienceJson,
        string? recipientFilter,
        string createdBy,
        EmailCampaignScheduleType? scheduleType = null,
        DateTime? scheduledAt = null,
        string? cronExpression = null
    )
    {
        var campaign = EmailCampaign.CreateDraft(
            name,
            subject,
            contentJson,
            contentHtml,
            audienceJson,
            recipientFilter,
            createdBy,
            scheduleType,
            scheduledAt,
            cronExpression
        );
        await _emailCampaignRepository.Add(campaign);
        return campaign;
    }

    public async Task<EmailCampaign?> GetById(EmailCampaignId id)
    {
        return await _emailCampaignRepository.FindById(id);
    }

    public async Task<List<EmailCampaign>> GetAll(string? statusFilter)
    {
        if (!string.IsNullOrEmpty(statusFilter) && EmailCampaignStatus.TryParse(statusFilter, out var status))
        {
            return await _emailCampaignRepository.GetByStatus(status);
        }
        return await _emailCampaignRepository.GetAll();
    }

    [TransactionalBoundary]
    public async Task UpdateDraft(
        EmailCampaignId id,
        string name,
        string subject,
        string contentJson,
        string? contentHtml,
        string audienceJson,
        string? recipientFilter,
        string modifiedBy,
        EmailCampaignScheduleType? scheduleType = null,
        DateTime? scheduledAt = null,
        string? cronExpression = null
    )
    {
        var campaign = await GetRequired(id);
        campaign.Update(name, subject, contentJson, contentHtml, audienceJson, recipientFilter, modifiedBy);

        if (scheduleType is not null)
        {
            campaign.UpdateScheduleFields(scheduleType, scheduledAt, cronExpression);
        }
    }

    [TransactionalBoundary]
    public async Task DeleteDraft(EmailCampaignId id)
    {
        var campaign = await GetRequired(id);

        if (campaign.Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Only drafts can be deleted.");

        campaign.SoftDelete();
    }

    [TransactionalBoundary]
    public async Task<EmailCampaign> DuplicateCampaign(EmailCampaignId sourceId, string createdBy)
    {
        var source = await GetRequired(sourceId);
        var duplicate = EmailCampaign.Duplicate(source, createdBy);
        await _emailCampaignRepository.Add(duplicate);
        return duplicate;
    }

    public async Task<AudienceResolutionResult> ResolveAudience(string audienceJson, string? recipientFilter)
    {
        var resolved = await ResolveCapabilityRecipients(audienceJson, recipientFilter);

        var capabilityResults = resolved.Select(cr => new AudienceCapabilityResult
        {
            Id = cr.Capability.Id.ToString(),
            Name = cr.Capability.Name,
            MemberCount = cr.Members.Count,
            Recipients = cr.Members.Select(m => new RecipientDto { Email = m.Email, DisplayName = m.DisplayName }).ToList(),
        }).ToList();

        return new AudienceResolutionResult
        {
            TotalCapabilities = resolved.Count,
            TotalRecipients = capabilityResults.Sum(c => c.MemberCount),
            Capabilities = capabilityResults,
        };
    }

    public async Task<List<EmailPreviewResult>> PreviewCampaign(EmailCampaignId id, string[]? capabilityIds)
    {
        var campaign = await GetRequired(id);

        List<Capability> capabilities;
        if (capabilityIds != null && capabilityIds.Length > 0)
        {
            capabilities = new List<Capability>();
            foreach (var capIdStr in capabilityIds)
            {
                if (CapabilityId.TryParse(capIdStr, out var capId))
                {
                    var cap = await _capabilityRepository.FindBy(capId);
                    if (cap != null) capabilities.Add(cap);
                }
            }
        }
        else
        {
            capabilities = await _capabilityFilterService.ResolveCapabilities(campaign.AudienceJson);
            // Limit preview to first 5 capabilities
            capabilities = capabilities.Take(5).ToList();
        }

        var previews = new List<EmailPreviewResult>();
        foreach (var cap in capabilities)
        {
            var memberships = await _membershipRepository.FindBy(cap.Id);
            var capMemberCount = memberships.Count();
            var html = campaign.ContentHtml ?? "";
            var context = await BuildRenderContext(cap, campaign.Name, capMemberCount);

            var renderedSubject = _templateRenderingService.RenderTemplate(campaign.Subject, context);
            var renderedHtml = _templateRenderingService.RenderTemplate(html, context);

            previews.Add(new EmailPreviewResult
            {
                CapabilityId = cap.Id.ToString(),
                CapabilityName = cap.Name,
                Subject = renderedSubject,
                Html = renderedHtml,
            });
        }

        return previews;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<SendCampaignResult> SendCampaign(EmailCampaignId id, string sentBy)
    {
        var campaign = await GetRequired(id);

        if (campaign.Status == EmailCampaignStatus.Sending || campaign.Status == EmailCampaignStatus.Sent)
            throw new InvalidOperationException("This campaign has already been sent or is currently sending.");

        var recipientCount = await ExecuteSend(campaign);
        campaign.MarkAsSent(sentBy);
        return new SendCampaignResult { TotalRecipients = recipientCount, Status = campaign.Status };
    }

    [TransactionalBoundary]
    public async Task CancelCampaign(EmailCampaignId id)
    {
        var campaign = await GetRequired(id);
        campaign.Cancel();
    }

    [TransactionalBoundary]
    public async Task RevertToDraft(EmailCampaignId id, string modifiedBy)
    {
        var campaign = await GetRequired(id);
        campaign.RevertToDraft(modifiedBy);
    }

    [TransactionalBoundary]
    public async Task ScheduleCampaign(
        EmailCampaignId id,
        EmailCampaignScheduleType scheduleType,
        DateTime? scheduledAt,
        string? cronExpression,
        string scheduledBy
    )
    {
        var campaign = await GetRequired(id);
        campaign.SetSchedule(scheduleType, scheduledAt, cronExpression);
        campaign.MarkAsScheduled(scheduledBy);
    }

    [TransactionalBoundary]
    public async Task MarkCampaignAsSending(EmailCampaignId id)
    {
        var campaign = await GetRequired(id);
        campaign.MarkAsSending();
    }

    [TransactionalBoundary, Outboxed]
    public async Task<SendCampaignResult> ExecuteScheduledCampaign(EmailCampaignId id)
    {
        var campaign = await GetRequired(id);

        if (campaign.Status != EmailCampaignStatus.Scheduled && campaign.Status != EmailCampaignStatus.Sending)
            return new SendCampaignResult { TotalRecipients = 0, Status = campaign.Status };

        var recipientCount = await ExecuteSend(campaign);
        campaign.MarkAsSent("system-scheduler");
        return new SendCampaignResult { TotalRecipients = recipientCount, Status = campaign.Status };
    }

    [TransactionalBoundary, Outboxed]
    public async Task<SendCampaignResult> ExecuteRecurringCampaign(EmailCampaignId id)
    {
        var campaign = await GetRequired(id);

        if (campaign.Status != EmailCampaignStatus.Scheduled)
            return new SendCampaignResult { TotalRecipients = 0, Status = campaign.Status };

        var recipientCount = await ExecuteSend(campaign);

        campaign.ResetToScheduled("system-scheduler");

        return new SendCampaignResult
        {
            TotalRecipients = recipientCount,
            Status = campaign.Status,
        };
    }

    public async Task<List<EmailCampaignExecution>> GetExecutions(EmailCampaignId id)
    {
        return await _executionRepository.GetByCampaignId(id);
    }

    public async Task<List<EmailCampaignRecipientLog>> GetRecipientLog(EmailCampaignId id)
    {
        return await _recipientLogRepository.GetByCampaignId(id);
    }

    [TransactionalBoundary, Outboxed]
    public async Task<RetryResult> RetryFailedRecipients(EmailCampaignId id, string retriedBy)
    {
        var campaign = await GetRequired(id);

        if (campaign.Status != EmailCampaignStatus.Sent && campaign.Status != EmailCampaignStatus.Failed)
            throw new InvalidOperationException("Can only retry recipients for sent or failed campaigns.");

        var failedLogs = await _recipientLogRepository.GetFailedByCampaignId(id);
        if (failedLogs.Count == 0)
            return new RetryResult { RetriedCount = 0, Status = "NoFailedRecipients" };

        foreach (var log in failedLogs)
        {
            log.ResetForRetry();

            campaign.RaiseSendRequestedEvent(
                log.Id.ToString(),
                log.Email,
                log.RenderedSubject,
                log.RenderedHtml
            );
        }

        return new RetryResult { RetriedCount = failedLogs.Count, Status = "Retried" };
    }

    [TransactionalBoundary]
    public async Task UpdateDeliveryStatus(EmailCampaignRecipientLogId recipientLogId, EmailCampaignRecipientStatus status, string? errorMessage)
    {
        var recipientLog = await _recipientLogRepository.FindById(recipientLogId);
        if (recipientLog == null)
            return;

        if (status == EmailCampaignRecipientStatus.Sent)
            recipientLog.MarkAsSent();
        else if (status == EmailCampaignRecipientStatus.Failed)
            recipientLog.MarkAsFailed(errorMessage);

        if (recipientLog.ExecutionId == null)
            return;

        var execution = await _executionRepository.FindById(recipientLog.ExecutionId);
        if (execution == null || execution.Status != EmailCampaignExecutionStatus.InProgress)
            return;

        var executionLogs = await _recipientLogRepository.GetByExecutionId(recipientLog.ExecutionId);
        var successCount = executionLogs.Count(l => l.Status == EmailCampaignRecipientStatus.Sent);
        var failureCount = executionLogs.Count(l => l.Status == EmailCampaignRecipientStatus.Failed);

        if (executionLogs.Count > 0 && successCount + failureCount == executionLogs.Count)
            execution.MarkCompleted(successCount, failureCount);
        else
            execution.UpdateProgress(successCount, failureCount);
    }

    public Task<List<TemplateVariable>> GetTemplateVariables() => Task.FromResult(_templateVariables);

    private async Task<int> ExecuteSend(EmailCampaign campaign)
    {
        campaign.MarkAsSending();

        var execution = EmailCampaignExecution.Create(campaign.Id, 0);
        await _executionRepository.Add(execution);

        var recipientCount = await SendToRecipients(campaign, execution.Id);
        execution.TotalRecipients = recipientCount;

        return recipientCount;
    }

    private async Task<int> SendToRecipients(EmailCampaign campaign, EmailCampaignExecutionId? executionId = null)
    {
        var resolved = await ResolveCapabilityRecipients(campaign.AudienceJson, campaign.RecipientFilter);
        var recipientLogs = new List<EmailCampaignRecipientLog>();
        var html = campaign.ContentHtml ?? "";

        foreach (var (cap, members, totalMemberships) in resolved)
        {
            var baseContext = await BuildRenderContext(cap, campaign.Name, totalMemberships);

            foreach (var member in members)
            {
                if (recipientLogs.Count >= EmailCampaign.MaxRecipientsPerCampaign)
                    break;

                if (string.IsNullOrEmpty(member.Email))
                    continue;

                var context = baseContext with { Member = member };
                var renderedSubject = _templateRenderingService.RenderTemplate(campaign.Subject, context);
                var renderedHtml = _templateRenderingService.RenderTemplate(html, context);

                var recipientLog = EmailCampaignRecipientLog.Create(
                    campaign.Id,
                    executionId,
                    cap.Id.ToString(),
                    cap.Name,
                    member.Id.ToString(),
                    member.Email,
                    renderedSubject,
                    renderedHtml
                );

                recipientLogs.Add(recipientLog);

                campaign.RaiseSendRequestedEvent(
                    recipientLog.Id.ToString(),
                    member.Email,
                    renderedSubject,
                    renderedHtml
                );
            }

            if (recipientLogs.Count >= EmailCampaign.MaxRecipientsPerCampaign)
                break;
        }

        await _recipientLogRepository.AddRange(recipientLogs);
        return recipientLogs.Count;
    }

    private sealed record CapabilityRecipients(Capability Capability, List<Member> Members, int TotalMemberships);

    private async Task<List<CapabilityRecipients>> ResolveCapabilityRecipients(string audienceJson, string? recipientFilter)
    {
        var capabilities = await _capabilityFilterService.ResolveCapabilities(audienceJson);
        var matchingRoleIds = await ResolveRoleIds(recipientFilter);
        var result = new List<CapabilityRecipients>(capabilities.Count);

        foreach (var cap in capabilities)
        {
            var allowedUserIds = await GetAllowedUsersForCapability(cap.Id, matchingRoleIds);
            var memberships = (await _membershipRepository.FindBy(cap.Id)).ToList();

            var members = new List<Member>();
            foreach (var membership in memberships)
            {
                if (allowedUserIds != null && !allowedUserIds.Contains(membership.UserId.ToString()))
                    continue;

                var member = await _memberRepository.FindBy(membership.UserId);
                if (member != null)
                    members.Add(member);
            }

            result.Add(new CapabilityRecipients(cap, members, memberships.Count));
        }

        return result;
    }

    private async Task<HashSet<RbacRoleId>?> ResolveRoleIds(string? recipientFilter)
    {
        if (string.IsNullOrEmpty(recipientFilter))
            return null;

        var allRoles = await _rbacApplicationService.GetAssignableRoles();
        return allRoles.Where(r => r.Name == recipientFilter).Select(r => r.Id).ToHashSet();
    }

    private async Task<HashSet<string>?> GetAllowedUsersForCapability(CapabilityId capabilityId, HashSet<RbacRoleId>? matchingRoleIds)
    {
        if (matchingRoleIds == null)
            return null;

        var allowedUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var grants = await _rbacApplicationService.GetRoleGrantsForCapability(capabilityId.ToString());
        foreach (var grant in grants)
        {
            if (grant.AssignedEntityType == AssignedEntityType.User && matchingRoleIds.Contains(grant.RoleId))
                allowedUserIds.Add(grant.AssignedEntityId);
        }
        return allowedUserIds;
    }

    private async Task<TemplateRenderContext> BuildRenderContext(Capability capability, string campaignName, int memberCount)
    {
        var capabilityId = capability.Id;

        var awsAccountTask = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAwsAccountRepository>();
            return await repo.FindBy(capabilityId);
        });

        var azureResourcesTask = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAzureResourceRepository>();
            return await repo.GetFor(capabilityId);
        });

        var requirementScoresTask = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRequirementsMetricService>();
            return await service.GetRequirementScoreAsync(capabilityId.ToString());
        });

        var pendingAppsTask = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ICapabilityMembershipApplicationQuery>();
            return await query.FindPendingBy(capabilityId);
        });

        await Task.WhenAll(awsAccountTask, azureResourcesTask, requirementScoresTask, pendingAppsTask);

        var (_, scores) = await requirementScoresTask;

        return new TemplateRenderContext
        {
            Capability = capability,
            CampaignName = campaignName,
            MemberCount = memberCount,
            AwsAccount = await awsAccountTask,
            AzureResources = await azureResourcesTask,
            RequirementScores = scores,
            PendingMembershipApplicationCount = (await pendingAppsTask).Count(),
        };
    }

    private async Task<EmailCampaign> GetRequired(EmailCampaignId id)
    {
        return await _emailCampaignRepository.FindById(id)
            ?? throw EntityNotFoundException<EmailCampaign>.UsingId(id.ToString());
    }
}
