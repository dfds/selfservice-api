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
    private readonly IUserFilterService _userFilterService;
    private readonly ITemplateRenderingService _templateRenderingService;
    private readonly IRbacApplicationService _rbacApplicationService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EmailCampaignApplicationService(
        IEmailCampaignRepository emailCampaignRepository,
        IEmailCampaignRecipientLogRepository recipientLogRepository,
        IEmailCampaignExecutionRepository executionRepository,
        ICapabilityRepository capabilityRepository,
        IMembershipRepository membershipRepository,
        IMemberRepository memberRepository,
        ICapabilityFilterService capabilityFilterService,
        IUserFilterService userFilterService,
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
        _userFilterService = userFilterService;
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
        string? cronExpression = null,
        EmailCampaignTargetType? targetType = null
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
            cronExpression,
            targetType
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
        string? cronExpression = null,
        EmailCampaignTargetType? targetType = null
    )
    {
        var campaign = await GetRequired(id);

        // TargetType is immutable after creation: the audience filter shape and the available
        // template variables depend on it, so a change would silently break the body & filter.
        if (targetType is not null && !ReferenceEquals(targetType, campaign.TargetType) && targetType != campaign.TargetType)
            throw new InvalidOperationException(
                "TargetType cannot be changed after a campaign is created. Duplicate the campaign instead."
            );

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

    [TransactionalBoundary, Outboxed]
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

        var capabilityResults = resolved
            .Select(cr => new AudienceCapabilityResult
            {
                Id = cr.Capability.Id.ToString(),
                Name = cr.Capability.Name,
                MemberCount = cr.Members.Count,
                Recipients = cr
                    .Members.Select(m => new RecipientDto { Email = m.Email, DisplayName = m.DisplayName })
                    .ToList(),
            })
            .ToList();

        return new AudienceResolutionResult
        {
            TotalCapabilities = resolved.Count,
            TotalRecipients = capabilityResults.Sum(c => c.MemberCount),
            Capabilities = capabilityResults,
        };
    }

    public async Task<UserAudienceResolutionResult> ResolveUserAudience(string audienceJson, string? recipientFilter)
    {
        var resolution = await _userFilterService.ResolveUsers(audienceJson);
        var matchingRoleIds = await ResolveRoleIds(recipientFilter);
        var members = await FilterMembersByRoles(resolution.Members, matchingRoleIds);

        return new UserAudienceResolutionResult
        {
            TotalRecipients = members.Count,
            Users = members
                .Select(m => new AudienceUserResult
                {
                    UserId = m.Id.ToString(),
                    Email = m.Email,
                    DisplayName = m.DisplayName,
                })
                .ToList(),
            UnmatchedEmails = resolution.UnmatchedEmails,
        };
    }

    private async Task<List<Member>> FilterMembersByRoles(List<Member> members, HashSet<RbacRoleId>? matchingRoleIds)
    {
        if (matchingRoleIds == null || matchingRoleIds.Count == 0)
            return members;

        var result = new List<Member>(members.Count);
        foreach (var member in members)
        {
            var grants = await _rbacApplicationService.GetRoleGrantsForUser(member.Id.ToString());
            if (grants.Any(g => matchingRoleIds.Contains(g.RoleId)))
                result.Add(member);
        }
        return result;
    }

    public async Task<List<UserEmailPreviewResult>> PreviewUserCampaign(EmailCampaignId id, string[]? userEmails)
    {
        var campaign = await GetRequired(id);

        List<Member> members;
        if (userEmails != null && userEmails.Length > 0)
        {
            var resolution = await _userFilterService.ResolveUsers(
                System.Text.Json.JsonSerializer.Serialize(new { mode = "specific", userEmails })
            );
            members = resolution.Members;
        }
        else
        {
            var resolution = await _userFilterService.ResolveUsers(campaign.AudienceJson);
            var matchingRoleIds = await ResolveRoleIds(campaign.RecipientFilter);
            var filtered = await FilterMembersByRoles(resolution.Members, matchingRoleIds);
            // Limit preview to first 5 users
            members = filtered.Take(5).ToList();
        }

        var previews = new List<UserEmailPreviewResult>();
        var html = campaign.ContentHtml ?? "";

        foreach (var member in members)
        {
            var userCapabilities = await LoadUserCapabilities(member.Id);
            var context = new TemplateRenderContext
            {
                Capability = null,
                Member = member,
                CampaignName = campaign.Name,
                UserCapabilities = userCapabilities,
            };

            previews.Add(
                new UserEmailPreviewResult
                {
                    UserId = member.Id.ToString(),
                    Email = member.Email,
                    DisplayName = member.DisplayName,
                    Subject = _templateRenderingService.RenderTemplate(campaign.Subject, context),
                    Html = _templateRenderingService.RenderTemplate(html, context),
                }
            );
        }

        return previews;
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
                    if (cap != null)
                        capabilities.Add(cap);
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

            previews.Add(
                new EmailPreviewResult
                {
                    CapabilityId = cap.Id.ToString(),
                    CapabilityName = cap.Name,
                    Subject = renderedSubject,
                    Html = renderedHtml,
                }
            );
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

        return new SendCampaignResult { TotalRecipients = recipientCount, Status = campaign.Status };
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

            campaign.RaiseSendRequestedEvent(log.Id.ToString(), log.Email, log.RenderedSubject, log.RenderedHtml);
        }

        return new RetryResult { RetriedCount = failedLogs.Count, Status = "Retried" };
    }

    [TransactionalBoundary]
    public async Task UpdateDeliveryStatus(
        EmailCampaignRecipientLogId recipientLogId,
        EmailCampaignRecipientStatus status,
        string? errorMessage
    )
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

    public Task<IReadOnlyList<TemplateVariable>> GetTemplateVariables(EmailCampaignTargetType? targetType = null) =>
        Task.FromResult(_templateRenderingService.GetVariableDefinitions(targetType));

    private async Task<int> ExecuteSend(EmailCampaign campaign)
    {
        campaign.MarkAsSending();

        var execution = EmailCampaignExecution.Create(campaign.Id, 0);
        await _executionRepository.Add(execution);

        var recipientCount = await SendToRecipients(campaign, execution.Id);
        execution.TotalRecipients = recipientCount;
        if (recipientCount == 0)
            execution.MarkCompleted(0, 0); // valid empty audience: nothing to deliver

        return recipientCount;
    }

    private Task<int> SendToRecipients(EmailCampaign campaign, EmailCampaignExecutionId? executionId = null) =>
        campaign.TargetType == EmailCampaignTargetType.User
            ? SendToUserRecipients(campaign, executionId)
            : SendToCapabilityRecipients(campaign, executionId);

    private async Task<int> SendToCapabilityRecipients(
        EmailCampaign campaign,
        EmailCampaignExecutionId? executionId
    )
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

    private async Task<int> SendToUserRecipients(EmailCampaign campaign, EmailCampaignExecutionId? executionId)
    {
        var resolution = await _userFilterService.ResolveUsers(campaign.AudienceJson);
        var matchingRoleIds = await ResolveRoleIds(campaign.RecipientFilter);
        var members = await FilterMembersByRoles(resolution.Members, matchingRoleIds);
        var recipientLogs = new List<EmailCampaignRecipientLog>();
        var html = campaign.ContentHtml ?? "";

        foreach (var member in members)
        {
            if (recipientLogs.Count >= EmailCampaign.MaxRecipientsPerCampaign)
                break;

            if (string.IsNullOrEmpty(member.Email))
                continue;

            var userCapabilities = await LoadUserCapabilities(member.Id);

            var context = new TemplateRenderContext
            {
                Capability = null,
                Member = member,
                CampaignName = campaign.Name,
                UserCapabilities = userCapabilities,
            };

            var renderedSubject = _templateRenderingService.RenderTemplate(campaign.Subject, context);
            var renderedHtml = _templateRenderingService.RenderTemplate(html, context);

            var recipientLog = EmailCampaignRecipientLog.Create(
                campaign.Id,
                executionId,
                capabilityId: null,
                capabilityName: null,
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

        await _recipientLogRepository.AddRange(recipientLogs);
        return recipientLogs.Count;
    }

    private async Task<List<UserCapabilityRef>> LoadUserCapabilities(UserId userId)
    {
        var memberships = await _membershipRepository.GetAllMembershipsForUserId(userId);
        var result = new List<UserCapabilityRef>(memberships.Count);

        foreach (var membership in memberships)
        {
            var cap = await _capabilityRepository.FindBy(membership.CapabilityId);
            if (cap == null)
                continue;
            var memberCount = (await _membershipRepository.FindBy(cap.Id)).Count();
            result.Add(new UserCapabilityRef { Capability = cap, MemberCount = memberCount });
        }

        return result;
    }

    private sealed record CapabilityRecipients(Capability Capability, List<Member> Members, int TotalMemberships);

    private async Task<List<CapabilityRecipients>> ResolveCapabilityRecipients(
        string audienceJson,
        string? recipientFilter
    )
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
        if (string.IsNullOrWhiteSpace(recipientFilter))
            return null;

        var requestedNames = recipientFilter
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requestedNames.Count == 0)
            return null;

        var allRoles = await _rbacApplicationService.GetAssignableRoles();
        var matchedRoles = allRoles.Where(r => requestedNames.Contains(r.Name)).ToList();
        var matchedNames = matchedRoles.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unmatched = requestedNames.Where(n => !matchedNames.Contains(n)).ToList();

        if (unmatched.Count > 0)
            throw new InvalidOperationException(
                $"The recipient filter role(s) '{string.Join(", ", unmatched)}' do not match any known RBAC role."
            );

        return matchedRoles.Select(r => r.Id).ToHashSet();
    }

    private async Task<HashSet<string>?> GetAllowedUsersForCapability(
        CapabilityId capabilityId,
        HashSet<RbacRoleId>? matchingRoleIds
    )
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

    private async Task<TemplateRenderContext> BuildRenderContext(
        Capability capability,
        string campaignName,
        int memberCount
    )
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
