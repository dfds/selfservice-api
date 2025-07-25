﻿using Amazon.EC2;
using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.Invitations;
using SelfService.Infrastructure.Api.Kafka;
using SelfService.Infrastructure.Api.Me;
using SelfService.Infrastructure.Api.MembershipApplications;
using SelfService.Infrastructure.Api.ReleaseNotes;
using SelfService.Infrastructure.Api.System;
using SelfService.Infrastructure.Api.Teams;
using static SelfService.Infrastructure.Api.Method;

namespace SelfService.Infrastructure.Api;

public class ApiResourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMembershipQuery _membershipQuery;
    private readonly ICapabilityDeletionStatusQuery _capabilityDeletionStatusQuery;
    private readonly IAwsAccountIdQuery _awsAccountIdQuery;

    public ApiResourceFactory(
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        IAuthorizationService authorizationService,
        IMembershipQuery membershipQuery,
        ICapabilityDeletionStatusQuery capabilityDeletionStatusQuery,
        IAwsAccountIdQuery awsAccountIdQuery
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _authorizationService = authorizationService;
        _membershipQuery = membershipQuery;
        _capabilityDeletionStatusQuery = capabilityDeletionStatusQuery;
        _awsAccountIdQuery = awsAccountIdQuery;
    }

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext ?? throw new ApplicationException("Not in a http request context!");

    private UserId CurrentUser
    {
        get
        {
            if (HttpContext.User.TryGetUserId(out var userId))
            {
                return userId;
            }

            throw new ApplicationException("Current user not available from http request context!");
        }
    }

    private PortalUser PortalUser => HttpContext.User.ToPortalUser();

    /// <summary>
    /// This returns a name for a controller that complies with the naming convention in ASP.NET where
    /// the "Controller" suffix should be omitted.
    /// </summary>
    /// <typeparam name="TController">The controller to extract the name from.</typeparam>
    /// <returns>Name on controller that adheres to the default naming convention (e.g. "FooController" -> "Foo").</returns>
    private static string GetNameOf<TController>()
        where TController : ControllerBase => typeof(TController).Name.Replace("Controller", "");

    public async Task<KafkaTopicApiResource> Convert(KafkaTopic topic)
    {
        var portalUser = HttpContext.User.ToPortalUser();

        var allowOnSelf = Allow.Get;
        if (await _authorizationService.CanDelete(portalUser, topic))
        {
            allowOnSelf += Delete;
        }

        var consumerAccessLevel = Allow.None;
        if (await _authorizationService.CanReadConsumers(portalUser, topic))
        {
            consumerAccessLevel += Get;
        }

        var messageContractsAccessLevel = Allow.None;
        if (await _authorizationService.CanReadMessageContracts(portalUser, topic))
        {
            messageContractsAccessLevel += Get;
        }

        /*
        if (await _authorizationService.CanAddMessageContract(portalUser, topic))
        {
            messageContractsAccessLevel += Post;
        }
        */

        var result = new KafkaTopicApiResource(
            id: topic.Id,
            name: topic.Name,
            description: topic.Description,
            capabilityId: topic.CapabilityId,
            kafkaClusterId: topic.KafkaClusterId,
            partitions: topic.Partitions,
            retention: topic.Retention,
            status: topic.Status.ToString(),
            links: new KafkaTopicApiResource.KafkaTopicLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetTopic),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = topic.Id }
                    ) ?? "",
                    rel: "self",
                    allow: allowOnSelf
                ),
                messageContracts: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetMessageContracts),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = topic.Id }
                    ) ?? "?",
                    rel: "related",
                    allow: messageContractsAccessLevel
                ),
                consumers: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetConsumers),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = topic.Id }
                    ) ?? "?",
                    rel: "related",
                    allow: consumerAccessLevel
                ),
                updateDescription: await _authorizationService.CanChange(portalUser, topic)
                    ? new ResourceActionLink(
                        href: _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaTopicController.ChangeTopicDescription),
                            controller: GetNameOf<KafkaTopicController>(),
                            values: new { id = topic.Id }
                        ) ?? "?",
                        method: "PUT"
                    )
                    : null
            )
        );

        return result;
    }

    private List<SelfAssessmentsApiResource> generateSelfAssessmentResources(
        List<SelfAssessment> selfAssessments,
        List<SelfAssessmentOption> selfAssessmentOptions,
        CapabilityId capabilityId
    )
    {
        var selfAssessmentsResources = new List<SelfAssessmentsApiResource>();

        foreach (var option in selfAssessmentOptions)
        {
            var selfAssessmentResource = new SelfAssessmentsApiResource(
                id: option.Id,
                shortName: option.ShortName,
                description: option.Description,
                documentationUrl: option.DocumentationUrl,
                status: null,
                assessedAt: null,
                links: new SelfAssessmentsApiResource.SelfAssessmentLinks(
                    updateSelfAssessment: new ResourceLink(
                        href: _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(CapabilityController.UpdateSelfAssessment),
                            controller: GetNameOf<CapabilityController>(),
                            values: new { id = capabilityId }
                        ) ?? "",
                        rel: "self",
                        allow: Allow.Post
                    )
                )
            );
            foreach (var selfAssessment in selfAssessments)
            {
                if (option.Id == selfAssessment.OptionId)
                {
                    selfAssessmentResource.AssessedAt = selfAssessment.RequestedAt;
                    selfAssessmentResource.Status = selfAssessment.Status;
                }
            }

            selfAssessmentsResources.Add(selfAssessmentResource);
        }

        return selfAssessmentsResources;
    }

    public List<SelfAssessmentOptionApiResource> Convert(List<SelfAssessmentOption> selfAssessmentOptions)
    {
        var selfAssessmentOptionsResources = new List<SelfAssessmentOptionApiResource>();

        foreach (var option in selfAssessmentOptions)
        {
            var optionResource = new SelfAssessmentOptionApiResource(
                id: option.Id,
                shortName: option.ShortName,
                description: option.Description,
                documentationUrl: option.DocumentationUrl,
                requestedAt: option.RequestedAt,
                requestedBy: option.RequestedBy,
                isActive: option.IsActive,
                links: new SelfAssessmentOptionApiResource.SelfAssessmentOptionLinks(selfAssessmentOption: null)
            );
            selfAssessmentOptionsResources.Add(optionResource);
        }

        return selfAssessmentOptionsResources;
    }

    public async Task<SelfAssessmentListApiResource> Convert(
        List<SelfAssessment> existingSelfAssessments,
        List<SelfAssessmentOption> possibleSelfAssessments,
        CapabilityId capabilityId
    )
    {
        var portalUser = HttpContext.User.ToPortalUser();

        var allowSelfAssessments = Allow.None;
        if (await _authorizationService.CanSelfAssess(portalUser.Id, capabilityId))
        {
            allowSelfAssessments += Get;
        }

        var result = new SelfAssessmentListApiResource(
            selfAssessments: generateSelfAssessmentResources(
                existingSelfAssessments,
                possibleSelfAssessments,
                capabilityId
            ),
            links: new SelfAssessmentListApiResource.SelfAssessmentListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetSelfAssessments),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capabilityId }
                    ) ?? "",
                    rel: "self",
                    allow: allowSelfAssessments
                )
            )
        );

        return result;
    }

    public CapabilityMembersApiResource Convert(string id, IEnumerable<Member> members)
    {
        return new CapabilityMembersApiResource(
            items: members.Select(Convert).ToArray(),
            links: new CapabilityMembersApiResource.CapabilityMembersLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<CapabilityController>(),
                        action: nameof(CapabilityController.GetCapabilityMembers),
                        values: new { id }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    private static MemberApiResource Convert(Member member)
    {
        return new MemberApiResource(
            id: member.Id.ToString(),
            name: member.DisplayName,
            email: member.Email
        // Note: [jandr] current design does not include the need for links
        );
    }

    public CapabilityListApiResource Convert(
        IEnumerable<Capability> capabilities,
        IEnumerable<Membership> currentUserMemberships
    )
    {
        var showDeleted = _authorizationService.CanViewDeletedCapabilities(PortalUser);
        capabilities = showDeleted
            ? capabilities
            : capabilities.Where(x => x.Status != CapabilityStatusOptions.Deleted);

        var capabilitiesSelected = capabilities.Select(ConvertToListItem).ToList();

        foreach (var capability in capabilitiesSelected)
        {
            var awsAccountId = _awsAccountIdQuery.FindBy(capability.Id);
            capability.AwsAccountId = awsAccountId == null ? "" : awsAccountId.ToString();

            var membership = currentUserMemberships.FirstOrDefault(x => x.CapabilityId == capability.Id);
            if (membership != null)
            {
                capability.UserIsMember = true;
            }
        }

        return new CapabilityListApiResource(
            items: capabilitiesSelected.ToArray(),
            links: new CapabilityListApiResource.CapabilityListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<CapabilityController>(),
                        action: nameof(CapabilityController.GetAllCapabilities)
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    // This conversion is used by Teams, so we don't need to check for membership
    public CapabilityListApiResource Convert(IEnumerable<Capability> capabilities)
    {
        var showDeleted = _authorizationService.CanViewDeletedCapabilities(PortalUser);
        capabilities = showDeleted
            ? capabilities
            : capabilities.Where(x => x.Status != CapabilityStatusOptions.Deleted);

        var capabilitiesSelected = capabilities.Select(ConvertToListItem).ToList();

        return new CapabilityListApiResource(
            items: capabilitiesSelected.ToArray(),
            links: new CapabilityListApiResource.CapabilityListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<CapabilityController>(),
                        action: nameof(CapabilityController.GetAllCapabilities)
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    private CapabilityListItemApiResource ConvertToListItem(Capability capability)
    {
        return new CapabilityListItemApiResource(
            id: capability.Id,
            name: capability.Name,
            createdAt: capability.CreatedAt,
            createdBy: capability.CreatedBy,
            status: capability.Status.ToString(),
            description: capability.Description,
            jsonMetadata: capability.JsonMetadata,
            awsAccountId: "",
            userIsMember: false, // this will be updated in the calling method
            links: new CapabilityListItemApiResource.CapabilityListItemLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityById),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capability.Id }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    private async Task<ResourceLink> CreateMembershipApplicationsLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.Get;

        if (await _authorizationService.CanApply(CurrentUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityMembershipApplications),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "related",
            allow: allowedInteractions
        );
    }

    private async Task<ResourceLink> CreateLeaveCapabilityLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.Get;

        if (await _authorizationService.CanLeave(CurrentUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.LeaveCapability),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "related",
            allow: allowedInteractions
        );
    }

    private ResourceLink CreateSelfLinkFor(Capability capability)
    {
        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityById),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: Allow.Get
        );
    }

    private async Task<ResourceLink> CreateMetadataLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;

        if (await _authorizationService.CanGetSetCapabilityJsonMetadata(PortalUser, capability.Id))
        {
            allowedInteractions += Get;
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityMetadata),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: allowedInteractions
        );
    }

    private async Task<ResourceLink> CreateSetRequiredMetadataLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;

        if (await _authorizationService.CanGetSetCapabilityJsonMetadata(PortalUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.SetCapabilityRequiredMetadata),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: allowedInteractions
        );
    }

    private async Task<ResourceLink> CreateSendInvitationsLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;

        if (await _authorizationService.CanInviteToCapability(CurrentUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.CreateInvitations),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: allowedInteractions
        );
    }

    private ResourceLink GetLinkedTeams(Capability capability)
    {
        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetLinkedTeams),
                controller: GetNameOf<CapabilityController>(),
                values: new { capabilityId = capability.Id }
            ) ?? "",
            rel: "self",
            allow: Allow.Get
        );
    }

    private ResourceLink CreateJoinLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;

        if (_authorizationService.CanBypassMembershipApprovals(PortalUser))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.Join),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: allowedInteractions
        );
    }

    private async Task<ResourceLink> CreateRequestDeletionLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanDeleteCapability(CurrentUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.RequestCapabilityDeletion),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id, user = CurrentUser }
            ) ?? "",
            rel: "self",
            allow: allowedInteractions
        );
    }

    private async Task<ResourceLink> CreateCancelDeletionRequestLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanDeleteCapability(CurrentUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.CancelCapabilityDeletionRequest),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: allowedInteractions
        );
    }

    private ResourceLink CreateMembersLinkFor(Capability capability)
    {
        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityMembers),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "related",
            allow: Allow.Get
        );
    }

    private ResourceLink CreateClusterAccessLinkFor(Capability capability)
    {
        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetKafkaClusterAccessList),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "related",
            allow: Allow.Get
        );
    }

    private async Task<ResourceLink> CreateAwsAccountLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;
        var capabilityMarkedForDeletion = await _capabilityDeletionStatusQuery.IsPendingDeletion(capability.Id);

        if (await _authorizationService.CanViewAwsAccount(CurrentUser, capability.Id))
        {
            allowedInteractions += Get;
        }

        if (
            await _authorizationService.CanRequestAwsAccount(CurrentUser, capability.Id) && !capabilityMarkedForDeletion
        )
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.RequestAwsAccount),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "related",
            allow: allowedInteractions
        );
    }

    private async Task<ResourceLink> CreateAwsAccountInformationLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;
        var capabilityMarkedForDeletion = await _capabilityDeletionStatusQuery.IsPendingDeletion(capability.Id);

        if (await _authorizationService.CanViewAwsAccountInformation(CurrentUser, capability.Id))
        {
            allowedInteractions += Get;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityAwsAccountInformation),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "related",
            allow: allowedInteractions
        );
    }

    private async Task<ResourceLink> CreateAzureResourcesLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;
        var capabilityMarkedForDeletion = await _capabilityDeletionStatusQuery.IsPendingDeletion(capability.Id);

        if (
            await _authorizationService.CanViewAzureResources(CurrentUser, capability.Id)
            && !capabilityMarkedForDeletion
        )
        {
            allowedInteractions += Get;
        }

        if (
            await _authorizationService.CanRequestAzureResources(CurrentUser, capability.Id)
            && !capabilityMarkedForDeletion
        )
        {
            allowedInteractions += Post;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.RequestCapabilityAzureResource),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "related",
            allow: allowedInteractions
        );
    }

    private ResourceLink CreateConfigurationLevelLinkFor(Capability capability)
    {
        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetConfigurationLevel),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: Allow.Get
        );
    }

    private async Task<ResourceLink> CreateSelfAssessmentsLinkFor(Capability capability)
    {
        var portalUser = HttpContext.User.ToPortalUser();

        var allowSelfAssessment = Allow.None;
        if (await _authorizationService.CanSelfAssess(portalUser.Id, capability.Id))
        {
            allowSelfAssessment += Get;
        }

        return new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetSelfAssessments),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }
            ) ?? "",
            rel: "self",
            allow: allowSelfAssessment
        );
    }

    public async Task<CapabilityDetailsApiResource> Convert(Capability capability)
    {
        return new CapabilityDetailsApiResource(
            id: capability.Id,
            name: capability.Name,
            createdAt: capability.CreatedAt,
            createdBy: capability.CreatedBy,
            status: capability.Status.ToString(),
            description: capability.Description,
            jsonMetadata: capability.JsonMetadata,
            jsonMetadataSchemaVersion: capability.JsonMetadataSchemaVersion,
            links: new CapabilityDetailsApiResource.CapabilityDetailsLinks(
                self: CreateSelfLinkFor(capability),
                members: CreateMembersLinkFor(capability),
                clusters: CreateClusterAccessLinkFor(capability),
                membershipApplications: await CreateMembershipApplicationsLinkFor(capability),
                leaveCapability: await CreateLeaveCapabilityLinkFor(capability),
                awsAccount: await CreateAwsAccountLinkFor(capability),
                awsAccountInformation: await CreateAwsAccountInformationLinkFor(capability),
                azureResources: await CreateAzureResourcesLinkFor(capability),
                requestCapabilityDeletion: await CreateRequestDeletionLinkFor(capability),
                cancelCapabilityDeletionRequest: await CreateCancelDeletionRequestLinkFor(capability),
                metadata: await CreateMetadataLinkFor(capability),
                setRequiredMetadata: await CreateSetRequiredMetadataLinkFor(capability),
                getLinkedTeams: GetLinkedTeams(capability),
                joinCapability: CreateJoinLinkFor(capability),
                sendInvitations: await CreateSendInvitationsLinkFor(capability),
                configurationLevel: CreateConfigurationLevelLinkFor(capability),
                selfAssessments: await CreateSelfAssessmentsLinkFor(capability)
            )
        );
    }

    public async Task<AwsAccountApiResource> Convert(AwsAccount account)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanViewAwsAccount(CurrentUser, account.CapabilityId))
        {
            allowedInteractions += Get;
        }

        return new AwsAccountApiResource(
            id: account.Id,
            accountId: account.Registration.AccountId?.ToString(),
            roleEmail: account.Registration.RoleEmail,
            @namespace: account.KubernetesLink.Namespace,
            status: Convert(account.Status),
            links: new AwsAccountApiResource.AwsAccountLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityAwsAccount),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = account.CapabilityId }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
    }

    public async Task<AwsAccountInformationApiResource> Convert(AwsAccountInformation information)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanViewAwsAccount(CurrentUser, information.CapabilityId))
        {
            allowedInteractions += Get;
        }
        return new AwsAccountInformationApiResource(
            id: information.Id,
            capabilityId: information.CapabilityId,
            vpcs: information.vpcs,
            links: new AwsAccountInformationApiResource.AwsAccountInformationLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityAwsAccountInformation),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = information.CapabilityId }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
    }

    private static string Convert(AwsAccountStatus accountStatus)
    {
        return accountStatus switch
        {
            AwsAccountStatus.Requested => "Requested",
            AwsAccountStatus.Pending => "Pending",
            AwsAccountStatus.Completed => "Completed",
            _ => "Pending",
        };
    }

    public async Task<AzureResourceApiResource> Convert(AzureResource resource)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanViewAzureResources(CurrentUser, resource.CapabilityId))
        {
            allowedInteractions += Get;
        }

        return new AzureResourceApiResource(
            id: resource.Id,
            environment: resource.Environment,
            links: new AzureResourceApiResource.AzureResourceLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityAzureResource),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { cid = resource.CapabilityId, rid = resource.Id }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
    }

    private static string Convert(AzureResourceStatus resourceStatus)
    {
        return resourceStatus switch
        {
            AzureResourceStatus.Requested => "Requested",
            AzureResourceStatus.Pending => "Pending",
            AzureResourceStatus.Completed => "Completed",
            _ => "Pending",
        };
    }

    public async Task<AzureResourcesApiResource> Convert(
        IEnumerable<AzureResource> resources,
        CapabilityId capabilityId
    )
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanViewAzureResources(CurrentUser, capabilityId))
        {
            allowedInteractions += Get;
        }

        var items = new AzureResourceApiResource[resources.Count()];
        for (var i = 0; i < resources.Count(); i++)
        {
            items[i] = await Convert(resources.ElementAt(i));
        }
        return new AzureResourcesApiResource(
            items: items,
            links: new AzureResourcesApiResource.AzureResourceListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityAzureResources),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capabilityId }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
    }

    public KafkaClusterApiResource Convert(KafkaCluster cluster)
    {
        var resourceLink = new ResourceLink(
            href: _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(KafkaClusterController.GetById),
                controller: GetNameOf<KafkaClusterController>(),
                values: new { id = cluster.Id }
            ) ?? "",
            rel: "self",
            allow: Allow.Get
        );

        return new KafkaClusterApiResource(
            id: cluster.Id,
            name: cluster.Name,
            description: cluster.Description,
            links: new KafkaClusterApiResource.KafkaClusterLinks(self: resourceLink)
        );
    }

    public KafkaClusterListApiResource Convert(IEnumerable<KafkaCluster> clusters)
    {
        return new KafkaClusterListApiResource(
            items: clusters.Select(Convert).ToArray(),
            links: new KafkaClusterListApiResource.KafkaClusterListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaClusterController.GetAllClusters),
                        controller: GetNameOf<KafkaClusterController>()
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    public MessageContractApiResource Convert(MessageContract messageContract)
    {
        return new MessageContractApiResource(
            id: messageContract.Id,
            messageType: messageContract.MessageType,
            description: messageContract.Description,
            example: messageContract.Example,
            schema: messageContract.Schema,
            kafkaTopicId: messageContract.KafkaTopicId,
            status: messageContract.Status,
            schemaVersion: messageContract.SchemaVersion,
            links: new MessageContractApiResource.MessageContractLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetSingleMessageContract),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = messageContract.KafkaTopicId, contractId = messageContract.Id }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                ),
                retry: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.RetryCreatingMessageContract),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = messageContract.KafkaTopicId, contractId = messageContract.Id }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.None
                )
            )
        );
    }

    public async Task<MessageContractListApiResource> Convert(
        IEnumerable<MessageContract> contracts,
        KafkaTopic parentKafkaTopic
    )
    {
        var allowedInteractions = Allow.Get;
        if (await _authorizationService.CanAddMessageContract(PortalUser, parentKafkaTopic))
        {
            allowedInteractions += Post;
        }

        var items = contracts.Select(Convert).ToList();

        foreach (var messageContractApiResource in items)
        {
            messageContractApiResource.Links.Retry.Allow = await _authorizationService.CanRetryCreatingMessageContract(
                PortalUser,
                messageContractApiResource.Id
            )
                ? Allow.Post
                : Allow.None;
        }

        return new MessageContractListApiResource(
            items: items.ToArray(),
            links: new MessageContractListApiResource.MessageContractListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetMessageContracts),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = parentKafkaTopic.Id }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
    }

    public async Task<ConsumersListApiResource> Convert(IEnumerable<string> consumers, KafkaTopic topic)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanReadConsumers(PortalUser, topic))
        {
            allowedInteractions += Get;
        }

        return new ConsumersListApiResource(
            items: consumers.ToArray(),
            links: new ConsumersListApiResource.ConsumersListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetConsumers),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { topicId = topic.Id, clusterId = topic.KafkaClusterId }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
    }

    public MembershipApplicationApiResource Convert(MembershipApplication application, UserId currentUser)
    {
        var isCurrentUserTheApplicant = application.Applicant == currentUser;

        // hide list of approvals if current user is the applicant
        var approvals = isCurrentUserTheApplicant ? Enumerable.Empty<MembershipApproval>() : application.Approvals;

        var allowedApprovalInteractions = Allow.None;
        if (!isCurrentUserTheApplicant)
        {
            allowedApprovalInteractions += Get;
            if (!application.HasApproved(currentUser))
            {
                allowedApprovalInteractions += Post;
                allowedApprovalInteractions += Delete;
            }
        }

        return new MembershipApplicationApiResource(
            id: application.Id.ToString(),
            applicant: application.Applicant,
            submittedAt: application.SubmittedAt.ToUniversalTime().ToString("O"),
            expiresOn: application.ExpiresOn.ToUniversalTime().ToString("O"),
            approvals: Convert(approvals, application.Id, allowedApprovalInteractions),
            links: new MembershipApplicationApiResource.MembershipApplicationLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(MembershipApplicationController.GetById),
                        controller: GetNameOf<MembershipApplicationController>(),
                        values: new { id = application.Id.ToString() }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    public MembershipApplicationThatUserCanApproveApiResource ConvertToMembershipApplicationThatUserCanApproveApiResource(
        MembershipApplication application,
        UserId currentUser
    )
    {
        var isCurrentUserTheApplicant = application.Applicant == currentUser;

        // hide list of approvals if current user is the applicant
        var approvals = isCurrentUserTheApplicant ? Enumerable.Empty<MembershipApproval>() : application.Approvals;

        var allowedApprovalInteractions = Allow.None;
        if (!isCurrentUserTheApplicant)
        {
            allowedApprovalInteractions += Get;
            if (!application.HasApproved(currentUser))
            {
                allowedApprovalInteractions += Post;
                allowedApprovalInteractions += Delete;
            }
        }

        return new MembershipApplicationThatUserCanApproveApiResource(
            id: application.Id.ToString(),
            capabilityId: application.CapabilityId.ToString(),
            applicant: application.Applicant,
            submittedAt: application.SubmittedAt.ToUniversalTime().ToString("O"),
            expiresOn: application.ExpiresOn.ToUniversalTime().ToString("O"),
            approvals: Convert(approvals, application.Id, allowedApprovalInteractions),
            links: new MembershipApplicationApiResource.MembershipApplicationLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(MembershipApplicationController.GetById),
                        controller: GetNameOf<MembershipApplicationController>(),
                        values: new { id = application.Id.ToString() }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    public OutstandingMembershipApplicationsForUserApiResource ConvertToOutstandingMembershipApplicationForUserApiResource(
        MembershipApplication application,
        UserId currentUser
    )
    {
        var isCurrentUserTheApplicant = application.Applicant == currentUser;

        // hide list of approvals if current user is the applicant
        var approvals = isCurrentUserTheApplicant ? Enumerable.Empty<MembershipApproval>() : application.Approvals;

        var allowedApprovalInteractions = Allow.None;
        if (isCurrentUserTheApplicant)
        {
            allowedApprovalInteractions += Get;
            allowedApprovalInteractions += Delete;
        }

        return new OutstandingMembershipApplicationsForUserApiResource(
            id: application.Id.ToString(),
            capabilityId: application.CapabilityId.ToString(),
            applicant: application.Applicant,
            submittedAt: application.SubmittedAt.ToUniversalTime().ToString("O"),
            expiresOn: application.ExpiresOn.ToUniversalTime().ToString("O"),
            approvals: Convert(approvals, application.Id, allowedApprovalInteractions),
            links: new MembershipApplicationApiResource.MembershipApplicationLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(MembershipApplicationController.GetById),
                        controller: GetNameOf<MembershipApplicationController>(),
                        values: new { id = application.Id.ToString() }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    private MembershipApprovalListApiResource Convert(
        IEnumerable<MembershipApproval> approvals,
        MembershipApplicationId parentApplicationId,
        Allow allowedInteractions
    )
    {
        return new MembershipApprovalListApiResource(
            items: approvals.Select(Convert).ToArray(),
            links: new MembershipApprovalListApiResource.MembershipApprovalListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(MembershipApplicationController.GetMembershipApplicationApprovals),
                        controller: GetNameOf<MembershipApplicationController>(),
                        values: new { id = parentApplicationId }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
    }

    private static MembershipApprovalApiResource Convert(MembershipApproval approval)
    {
        return new MembershipApprovalApiResource(
            id: approval.Id.ToString("N"),
            approvedBy: approval.ApprovedBy,
            approvedAt: approval.ApprovedAt.ToUniversalTime().ToString("O")
        );
    }

    public async Task<KafkaClusterAccessListApiResource> Convert(
        CapabilityId capabilityId,
        IEnumerable<KafkaCluster> clusters
    )
    {
        IList<KafkaClusterAccessListItemApiResource> items = new List<KafkaClusterAccessListItemApiResource>();

        foreach (var cluster in clusters)
        {
            var isMemberOfCapability = await _membershipQuery.HasActiveMembership(CurrentUser, capabilityId);
            var capabilityHasKafkaClusterAccess = await _authorizationService.HasAccess(capabilityId, cluster.Id);
            var capabilityMarkedForDeletion = await _capabilityDeletionStatusQuery.IsPendingDeletion(capabilityId);
            var accessAllow = Allow.None;
            var requestAccessAllow = Allow.None;
            var createTopicAllow = Allow.None;

            if (isMemberOfCapability && !capabilityMarkedForDeletion)
            {
                if (capabilityHasKafkaClusterAccess)
                {
                    createTopicAllow += Post;
                    accessAllow += Get;
                }
                else
                {
                    requestAccessAllow += Post;
                }
            }

            items.Add(
                new KafkaClusterAccessListItemApiResource(
                    id: cluster.Id,
                    name: cluster.Name,
                    description: cluster.Description,
                    links: new KafkaClusterAccessListItemApiResource.KafkaClusterAccessListItem(
                        access: new ResourceLink(
                            href: _linkGenerator.GetUriByAction(
                                httpContext: HttpContext,
                                action: nameof(CapabilityController.GetKafkaClusterAccess),
                                controller: GetNameOf<CapabilityController>(),
                                values: new { id = capabilityId, clusterId = cluster.Id }
                            ) ?? "",
                            rel: "related",
                            allow: accessAllow
                        ),
                        topics: new ResourceLink(
                            href: _linkGenerator.GetUriByAction(
                                httpContext: HttpContext,
                                action: nameof(KafkaTopicController.GetAllTopics),
                                controller: GetNameOf<KafkaTopicController>(),
                                values: new
                                {
                                    capabilityId,
                                    clusterId = cluster.Id,
                                    includePrivate = true,
                                }
                            ) ?? "",
                            rel: "related",
                            allow: Allow.Get
                        ),
                        schemas: new ResourceLink(
                            href: _linkGenerator.GetUriByAction(
                                httpContext: HttpContext,
                                action: nameof(KafkaSchemaController.ListSchemas),
                                controller: GetNameOf<KafkaSchemaController>(),
                                values: new { subjectPrefix = capabilityId, clusterId = cluster.Id }
                            ) ?? "",
                            rel: "related",
                            allow: Allow.Get
                        ),
                        requestAccess: new ResourceLink(
                            href: _linkGenerator.GetUriByAction(
                                httpContext: HttpContext,
                                action: nameof(CapabilityController.RequestKafkaClusterAccess),
                                controller: GetNameOf<CapabilityController>(),
                                values: new { id = capabilityId, clusterId = cluster.Id }
                            ) ?? "",
                            rel: "self",
                            allow: requestAccessAllow
                        ),
                        createTopic: new ResourceLink(
                            href: _linkGenerator.GetUriByAction(
                                httpContext: HttpContext,
                                action: nameof(CapabilityController.AddCapabilityTopic),
                                controller: GetNameOf<CapabilityController>(),
                                values: new { id = capabilityId }
                            ) ?? "",
                            rel: "self",
                            allow: createTopicAllow
                        )
                    )
                )
            );
        }

        var resource = new KafkaClusterAccessListApiResource(
            items: items.ToArray(),
            links: new KafkaClusterAccessListApiResource.KafkaClusterAccessList(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetKafkaClusterAccessList),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capabilityId }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
        return resource;
    }

    public Task<MembershipApplicationThatUserCanApproveListApiResource> Convert(
        IEnumerable<MembershipApplication> applications,
        UserId currentUser
    )
    {
        var resource = new MembershipApplicationThatUserCanApproveListApiResource(
            items: applications
                .Select(application =>
                    ConvertToMembershipApplicationThatUserCanApproveApiResource(application, currentUser)
                )
                .ToArray(),
            links: new MembershipApplicationThatUserCanApproveListApiResource.MembershipApplicationListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<MembershipApplicationController>(),
                        action: nameof(MembershipApplicationController.MembershipsThatUserCanApprove)
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );

        return Task.FromResult(resource);
    }

    public Task<OutstandingMembershipApplicationsForUserListApiResource> ConvertOutstandingApplications(
        IEnumerable<MembershipApplication> applications,
        UserId currentUser
    )
    {
        var resource = new OutstandingMembershipApplicationsForUserListApiResource(
            items: applications
                .Select(application =>
                    ConvertToOutstandingMembershipApplicationForUserApiResource(application, currentUser)
                )
                .ToArray(),
            links: new OutstandingMembershipApplicationsForUserListApiResource.MembershipApplicationListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<MembershipApplicationController>(),
                        action: nameof(MembershipApplicationController.MyOutstandingApplications)
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );

        return Task.FromResult(resource);
    }

    public async Task<MembershipApplicationListApiResource> Convert(
        CapabilityId capabilityId,
        IEnumerable<MembershipApplication> applications,
        UserId userId
    )
    {
        var allowedInteractions = Allow.Get;
        if (await _authorizationService.CanApply(CurrentUser, capabilityId))
        {
            allowedInteractions += Post;
        }

        var resource = new MembershipApplicationListApiResource(
            items: applications.Select(application => Convert(application, userId)).ToArray(),
            links: new MembershipApplicationListApiResource.MembershipApplicationListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<CapabilityController>(),
                        action: nameof(CapabilityController.GetCapabilityMembershipApplications),
                        values: new { id = capabilityId }
                    ) ?? "",
                    rel: "self",
                    allow: allowedInteractions
                )
            )
        );
        return resource;
    }

    public async Task<KafkaTopicListApiResource> Convert(
        IEnumerable<KafkaTopic> topics,
        IEnumerable<KafkaCluster> clusters,
        KafkaTopicQueryParams queryParams
    )
    {
        var list = new List<KafkaTopicApiResource>();
        foreach (var topic in topics)
        {
            var apiResource = await Convert(topic);
            list.Add(apiResource);
        }

        return new KafkaTopicListApiResource(
            items: list.ToArray(),
            embedded: new KafkaTopicListApiResource.KafkaTopicListEmbeddedResources(kafkaClusters: Convert(clusters)),
            links: new KafkaTopicListApiResource.KafkaTopicListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<KafkaTopicController>(),
                        action: nameof(KafkaTopicController.GetAllTopics),
                        values: queryParams
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    public MyProfileApiResource Convert(
        UserId userId,
        IEnumerable<Capability> capabilities,
        Member? member,
        bool isDevelopment
    )
    {
        return new MyProfileApiResource(
            id: userId,
            capabilities: capabilities.Select(ConvertToListItem),
            autoReloadTopics: !isDevelopment,
            personalInformation: member != null
                ? new PersonalInformationApiResource { Name = member.DisplayName ?? "", Email = member.Email }
                : PersonalInformationApiResource.Empty,
            links: new MyProfileApiResource.MyProfileLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<MeController>(),
                        action: nameof(MeController.GetMe)
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                ),
                personalInformation: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<MeController>(),
                        action: nameof(MeController.UpdatePersonalInformation)
                    ) ?? "",
                    rel: "related",
                    allow: Allow.Put
                ),
                portalVisits: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<PortalVisitController>(),
                        action: nameof(PortalVisitController.RegisterVisit)
                    ) ?? "",
                    rel: "related",
                    allow: Allow.Post
                ),
                topVisitors: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<SystemController>(),
                        action: nameof(SystemController.GetTopVisitors)
                    ) ?? "",
                    rel: "related",
                    allow: Allow.Get
                ),
                invitationsLinks: new MyProfileApiResource.InvitationsLinks(
                    capalityInvitations: new ResourceLink(
                        href: _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            controller: GetNameOf<InvitationController>(),
                            action: nameof(InvitationController.GetActiveInvitations),
                            values: new { targetType = "Capability" }
                        ) ?? "",
                        rel: "related",
                        allow: Allow.Get
                    )
                )
            )
        );
    }

    public TeamApiResource Convert(Team team)
    {
        return new TeamApiResource(
            team.Id,
            team.Name,
            team.Description,
            team.CreatedBy,
            team.CreatedAt.ToUniversalTime().ToString("O"),
            new TeamApiResource.TeamLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(TeamController.GetTeam),
                        controller: GetNameOf<TeamController>(),
                        values: new { id = team.Id }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                ),
                capabilities: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(TeamController.GetLinkedCapabilities),
                        controller: GetNameOf<TeamController>(),
                        values: new { id = team.Id }
                    ) ?? "",
                    rel: "related",
                    allow: Allow.Get
                ),
                members: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(TeamController.GetMembers),
                        controller: GetNameOf<TeamController>(),
                        values: new { id = team.Id }
                    ) ?? "",
                    rel: "related",
                    allow: Allow.Get
                )
            )
        );
    }

    public TeamListApiResource Convert(List<Team> teams)
    {
        return new TeamListApiResource(
            items: teams.Select(Convert).ToArray(),
            links: new TeamListApiResource.TeamListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(TeamController.GetAllTeams),
                        controller: GetNameOf<TeamController>()
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    public InvitationListApiResource Convert(IEnumerable<Invitation> invitations, string userId)
    {
        return new InvitationListApiResource(
            items: invitations.Select(Convert).ToArray(),
            links: new InvitationListApiResource.InvitationListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<InvitationController>(),
                        action: nameof(InvitationController.GetActiveInvitations),
                        values: new { userId }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    public InvitationListApiResource Convert(IEnumerable<Invitation> invitations, string userId, string targetType)
    {
        return new InvitationListApiResource(
            items: invitations.Select(Convert).ToArray(),
            links: new InvitationListApiResource.InvitationListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<InvitationController>(),
                        action: nameof(InvitationController.GetActiveInvitations),
                        values: new { userId, targetType }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                )
            )
        );
    }

    public InvitationApiResource Convert(Invitation invitation)
    {
        return new InvitationApiResource(
            invitation.Id,
            invitation.Invitee,
            invitation.Description,
            invitation.CreatedBy,
            invitation.CreatedAt.ToUniversalTime().ToString("O"),
            new InvitationApiResource.InvitationLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(InvitationController.GetInvitation),
                        controller: GetNameOf<InvitationController>(),
                        values: new { id = invitation.Id }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                ),
                accept: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(InvitationController.AcceptInvitation),
                        controller: GetNameOf<InvitationController>(),
                        values: new { id = invitation.Id }
                    ) ?? "",
                    rel: "related",
                    allow: Allow.Post
                ),
                decline: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(InvitationController.DeclineInvitation),
                        controller: GetNameOf<InvitationController>(),
                        values: new { id = invitation.Id }
                    ) ?? "",
                    rel: "related",
                    allow: Allow.Post
                )
            )
        );
    }

    public ReleaseNoteApiResource Convert(ReleaseNote releaseNote)
    {
        var portalUser = HttpContext.User.ToPortalUser();
        var allowedToggleInteractions = Allow.None;
        if (_authorizationService.IsAuthorizedToToggleReleaseNoteIsActive(portalUser))
        {
            allowedToggleInteractions += Post;
        }
        var allowedRemoveInteractions = Allow.None;
        if (_authorizationService.IsAuthorizedToRemoveReleaseNote(portalUser))
        {
            allowedRemoveInteractions += Delete;
        }

        return new ReleaseNoteApiResource(
            id: releaseNote.Id.ToString(),
            title: releaseNote.Title,
            releaseDate: releaseNote.ReleaseDate,
            content: releaseNote.Content,
            createdAt: releaseNote.CreatedAt,
            createdBy: releaseNote.CreatedBy,
            modifiedAt: releaseNote.ModifiedAt,
            modifiedBy: releaseNote.ModifiedBy,
            isActive: releaseNote.IsActive,
            links: new ReleaseNoteApiResource.ReleaseNoteLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(ReleaseNotesController.GetReleaseNote),
                        controller: GetNameOf<ReleaseNotesController>(),
                        values: new { id = releaseNote.Id }
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                ),
                toggleIsActive: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(ReleaseNotesController.ToggleIsActive),
                        controller: GetNameOf<ReleaseNotesController>(),
                        values: new { id = releaseNote.Id }
                    ) ?? "",
                    rel: "toggleIsActive",
                    allow: allowedToggleInteractions
                ),
                remove: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(ReleaseNotesController.RemoveReleaseNote),
                        controller: GetNameOf<ReleaseNotesController>(),
                        values: new { id = releaseNote.Id }
                    ) ?? "",
                    rel: "remove",
                    allow: allowedRemoveInteractions
                )
            )
        );
    }

    public ReleaseNoteListApiResource Convert(IEnumerable<ReleaseNote> releaseNotes)
    {
        var portalUser = HttpContext.User.ToPortalUser();
        var allowedInteractions = Allow.None;
        if (_authorizationService.IsAuthorizedToCreateReleaseNotes(portalUser))
        {
            allowedInteractions += Post;
        }

        var items = releaseNotes.Select(Convert).ToArray();

        return new ReleaseNoteListApiResource(
            items: items,
            links: new ReleaseNoteListApiResource.ReleaseNoteListLinks(
                self: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(ReleaseNotesController.GetReleaseNotes),
                        controller: GetNameOf<ReleaseNotesController>()
                    ) ?? "",
                    rel: "self",
                    allow: Allow.Get
                ),
                createReleaseNote: new ResourceLink(
                    href: _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(ReleaseNotesController.CreateReleaseNote),
                        controller: GetNameOf<ReleaseNotesController>()
                    ) ?? "",
                    rel: "create",
                    allow: allowedInteractions
                )
            )
        );
    }
}
