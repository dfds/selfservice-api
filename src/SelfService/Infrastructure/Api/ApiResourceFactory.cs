using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.Kafka;
using SelfService.Infrastructure.Api.Me;
using SelfService.Infrastructure.Api.MembershipApplications;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Metrics;
using SelfService.Infrastructure.Api.System;
using static SelfService.Infrastructure.Api.Method;

namespace SelfService.Infrastructure.Api;

public class ApiResourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMembershipQuery _membershipQuery;

    public ApiResourceFactory(
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        IAuthorizationService authorizationService,
        IMembershipQuery membershipQuery
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _authorizationService = authorizationService;
        _membershipQuery = membershipQuery;
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

        if (await _authorizationService.CanAddMessageContract(portalUser, topic))
        {
            messageContractsAccessLevel += Post;
        }

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

    public CapabilityListApiResource Convert(IEnumerable<Capability> capabilities)
    {
        return new CapabilityListApiResource(
            items: capabilities.Select(ConvertToListItem).ToArray(),
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

    public CapabilityListItemApiResource ConvertToListItem(Capability capability)
    {
        return new CapabilityListItemApiResource(
            id: capability.Id,
            name: capability.Name,
            status: capability.Status.ToString(),
            description: capability.Description,
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

        if (await _authorizationService.CanViewAwsAccount(CurrentUser, capability.Id))
        {
            allowedInteractions += Get;

            if (await _authorizationService.CanRequestAwsAccount(CurrentUser, capability.Id))
            {
                allowedInteractions += Post;
            }
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

    public async Task<CapabilityDetailsApiResource> Convert(Capability capability)
    {
        return new CapabilityDetailsApiResource(
            id: capability.Id,
            name: capability.Name,
            status: capability.Status.ToString(),
            description: capability.Description,
            links: new CapabilityDetailsApiResource.CapabilityDetailsLinks(
                self: CreateSelfLinkFor(capability),
                members: CreateMembersLinkFor(capability),
                clusters: CreateClusterAccessLinkFor(capability),
                membershipApplications: await CreateMembershipApplicationsLinkFor(capability),
                leaveCapability: await CreateLeaveCapabilityLinkFor(capability),
                awsAccount: await CreateAwsAccountLinkFor(capability),
                requestCapabilityDeletion: await CreateRequestDeletionLinkFor(capability),
                cancelCapabilityDeletionRequest: await CreateCancelDeletionRequestLinkFor(capability)
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

    private static string Convert(AwsAccountStatus accountStatus)
    {
        return accountStatus switch
        {
            AwsAccountStatus.Requested => "Requested",
            AwsAccountStatus.Pending => "Pending",
            AwsAccountStatus.Completed => "Completed",
            _ => "Pending"
        };
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

        return new MessageContractListApiResource(
            items: contracts.Select(Convert).ToArray(),
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

            var accessAllow = Allow.None;
            var requestAccessAllow = Allow.None;
            var createTopicAllow = Allow.None;

            if (isMemberOfCapability)
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
                                    includePrivate = true
                                }
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
                )
            )
        );
    }
}
