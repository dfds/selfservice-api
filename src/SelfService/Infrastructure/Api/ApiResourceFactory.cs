using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.Kafka;
using SelfService.Infrastructure.Api.Me;
using SelfService.Infrastructure.Api.MembershipApplications;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.System;
using static SelfService.Infrastructure.Api.Method;

namespace SelfService.Infrastructure.Api;

public class ApiResourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMembershipQuery _membershipQuery;

    public ApiResourceFactory(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator,
        IAuthorizationService authorizationService, IMembershipQuery membershipQuery)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _authorizationService = authorizationService;
        _membershipQuery = membershipQuery;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ??
                                       throw new ApplicationException("Not in a http request context!");

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
    private static string GetNameOf<TController>() where TController : ControllerBase
        => typeof(TController).Name.Replace("Controller", "");

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

        var result = new KafkaTopicApiResource
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description,
            CapabilityId = topic.CapabilityId,
            KafkaClusterId = topic.KafkaClusterId,
            Partitions = topic.Partitions,
            Retention = topic.Retention,
            Status = topic.Status.ToString(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetTopic),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = topic.Id }) ?? "",
                    Rel = "self",
                    Allow = allowOnSelf
                },
                MessageContracts = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetMessageContracts),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = topic.Id }) ?? "?",
                    Rel = "related",
                    Allow = messageContractsAccessLevel
                },
                Consumers = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetConsumers),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = topic.Id }) ?? "?",
                    Rel = "related",
                    Allow = consumerAccessLevel
                },
                UpdateDescription = await _authorizationService.CanChange(portalUser, topic)
                    ? new ResourceActionLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaTopicController.ChangeTopicDescription),
                            controller: GetNameOf<KafkaTopicController>(),
                            values: new { id = topic.Id }) ?? "?",
                        Method = "PUT",
                    }
                    : null
            }
        };

        return result;
    }

    public CapabilityMembersApiResource Convert(string id, IEnumerable<Member> members)
    {
        return new CapabilityMembersApiResource
        {
            Items = members
                .Select(Convert)
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<CapabilityController>(),
                        action: nameof(CapabilityController.GetCapabilityMembers), values: new { id }) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    private static MemberApiResource Convert(Member member)
    {
        return new MemberApiResource
        {
            Id = member.Id.ToString(),
            Name = member.DisplayName,
            Email = member.Email,

            // Note: [jandr] current design does not include the need for links
        };
    }

    public CapabilityListApiResource Convert(IEnumerable<Capability> capabilities)
    {
        return new CapabilityListApiResource
        {
            Items = capabilities
                .Select(ConvertToListItem)
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<CapabilityController>(),
                        action: nameof(CapabilityController.GetAllCapabilities)) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    public CapabilityListItemApiResource ConvertToListItem(Capability capability)
    {
        return new CapabilityListItemApiResource
        {
            Id = capability.Id,
            Name = capability.Name,
            Description = capability.Description,
            Links =
            {
                Self =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityById),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capability.Id }) ?? "",
                    Rel = "self",
                    Allow = { Get }
                },
            },
        };
    }

    private async Task<ResourceLink> CreateMembershipApplicationsLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.Get;

        if (await _authorizationService.CanApply(CurrentUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityMembershipApplications),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "related",
            Allow = allowedInteractions
        };
    }

    private async Task<ResourceLink> CreateLeaveCapabilityLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.Get;

        if (await _authorizationService.CanLeave(CurrentUser, capability.Id))
        {
            allowedInteractions += Post;
        }

        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.LeaveCapability),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "related",
            Allow = allowedInteractions
        };
    }

    private ResourceLink CreateSelfLinkFor(Capability capability)
    {
        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityById),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "self",
            Allow = { Get }
        };
    }

    private ResourceLink CreateMembersLinkFor(Capability capability)
    {
        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityMembers),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "related",
            Allow = { Get }
        };
    }

    private ResourceLink CreateClusterAccessLinkFor(Capability capability)
    {
        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetKafkaClusterAccessList),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "related",
            Allow = { Get }
        };
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

        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.RequestAwsAccount),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "related",
            Allow = allowedInteractions
        };
    }

    private ResourceLink CreateCostsLinkFor(Capability capability)
    {
        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCosts),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "self",
            Allow = { Get }
        };
    }

    public async Task<CapabilityDetailsApiResource> Convert(Capability capability)
    {
        return new CapabilityDetailsApiResource
        {
            Id = capability.Id,
            Name = capability.Name,
            Description = capability.Description,
            Links =
            {
                Self = CreateSelfLinkFor(capability),
                Members = CreateMembersLinkFor(capability),
                Clusters = CreateClusterAccessLinkFor(capability),
                MembershipApplications = await CreateMembershipApplicationsLinkFor(capability),
                LeaveCapability = await CreateLeaveCapabilityLinkFor(capability),
                AwsAccount = await CreateAwsAccountLinkFor(capability),
                Costs = CreateCostsLinkFor(capability),
            },
        };
    }

    public async Task<AwsAccountApiResource> Convert(AwsAccount account)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanViewAwsAccount(CurrentUser, account.CapabilityId))
        {
            allowedInteractions += Get;
        }

        return new AwsAccountApiResource
        {
            Id = account.Id,
            AccountId = account.Registration.AccountId?.ToString(),
            RoleEmail = account.Registration.RoleEmail,
            Namespace = account.KubernetesLink.Namespace,
            Status = Convert(account.Status),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityAwsAccount),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = account.CapabilityId }) ?? "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        };
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
        var resourceLink = new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(KafkaClusterController.GetById),
                controller: GetNameOf<KafkaClusterController>(),
                values: new { id = cluster.Id }) ?? "",
            Rel = "self",
            Allow = { Get }
        };

        return new KafkaClusterApiResource
        {
            Id = cluster.Id,
            Name = cluster.Name,
            Description = cluster.Description,
            Links =
            {
                Self = resourceLink
            }
        };
    }

    public KafkaClusterListApiResource Convert(IEnumerable<KafkaCluster> clusters)
    {
        return new KafkaClusterListApiResource
        {
            Items = clusters
                .Select(Convert)
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaClusterController.GetAllClusters),
                        controller: GetNameOf<KafkaClusterController>()) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    public MessageContractApiResource Convert(MessageContract messageContract)
    {
        return new MessageContractApiResource
        {
            Id = messageContract.Id,
            MessageType = messageContract.MessageType,
            Description = messageContract.Description,
            Example = messageContract.Example,
            Schema = messageContract.Schema,
            KafkaTopicId = messageContract.KafkaTopicId,
            Status = messageContract.Status,
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetSingleMessageContract),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = messageContract.KafkaTopicId, contractId = messageContract.Id }) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    public async Task<MessageContractListApiResource> Convert(IEnumerable<MessageContract> contracts,
        KafkaTopic parentKafkaTopic)
    {
        var allowedInteractions = Allow.Get;
        if (await _authorizationService.CanAddMessageContract(PortalUser, parentKafkaTopic))
        {
            allowedInteractions += Post;
        }

        return new MessageContractListApiResource
        {
            Items = contracts
                .Select(Convert)
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetMessageContracts),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { id = parentKafkaTopic.Id }) ?? "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        };
    }

    public async Task<ConsumersListApiResource> Convert(IEnumerable<string> consumers,
        KafkaTopic topic)
    {
        var allowedInteractions = Allow.None;
        if (await _authorizationService.CanReadConsumers(PortalUser, topic))
        {
            allowedInteractions += Get;
        }

        return new ConsumersListApiResource
        {
            Items = consumers
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetConsumers),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new { topicId = topic.Id, clusterId = topic.KafkaClusterId }) ?? "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        };
    }

    public MembershipApplicationApiResource Convert(MembershipApplication application, UserId currentUser)
    {
        var isCurrentUserTheApplicant = application.Applicant == currentUser;

        // hide list of approvals if current user is the applicant
        var approvals = isCurrentUserTheApplicant
            ? Enumerable.Empty<MembershipApproval>()
            : application.Approvals;

        var allowedApprovalInteractions = Allow.None;
        if (!isCurrentUserTheApplicant)
        {
            allowedApprovalInteractions += Get;
            if (!application.HasApproved(currentUser))
            {
                allowedApprovalInteractions += Post;
            }
        }

        return new MembershipApplicationApiResource
        {
            Id = application.Id.ToString(),
            Applicant = application.Applicant,
            SubmittedAt = application.SubmittedAt.ToUniversalTime().ToString("O"),
            ExpiresOn = application.ExpiresOn.ToUniversalTime().ToString("O"),
            Approvals = Convert(approvals, application.Id, allowedApprovalInteractions),
            Links =
            {
                Self =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(MembershipApplicationController.GetById),
                        controller: GetNameOf<MembershipApplicationController>(),
                        values: new { id = application.Id.ToString() }) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    private MembershipApprovalListApiResource Convert(IEnumerable<MembershipApproval> approvals,
        MembershipApplicationId parentApplicationId, Allow allowedInteractions)
    {
        return new MembershipApprovalListApiResource
        {
            Items = approvals
                .Select(Convert)
                .ToArray(),
            Links =
            {
                Self =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(MembershipApplicationController.GetMembershipApplicationApprovals),
                        controller: GetNameOf<MembershipApplicationController>(),
                        values: new { id = parentApplicationId }) ?? "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        };
    }

    private static MembershipApprovalApiResource Convert(MembershipApproval approval)
    {
        return new MembershipApprovalApiResource
        {
            Id = approval.Id.ToString("N"),
            ApprovedBy = approval.ApprovedBy,
            ApprovedAt = approval.ApprovedAt.ToUniversalTime().ToString("O")
        };
    }

    public async Task<KafkaClusterAccessListApiResource> Convert(CapabilityId capabilityId,
        IEnumerable<KafkaCluster> clusters)
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

            items.Add(new KafkaClusterAccessListItemApiResource
            {
                Id = cluster.Id,
                Name = cluster.Name,
                Description = cluster.Description,
                Links =
                {
                    Access =
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(CapabilityController.GetKafkaClusterAccess),
                            controller: GetNameOf<CapabilityController>(),
                            values: new { id = capabilityId, clusterId = cluster.Id }) ?? "",
                        Rel = "related",
                        Allow = accessAllow
                    },
                    Topics =
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaTopicController.GetAllTopics),
                            controller: GetNameOf<KafkaTopicController>(),
                            values: new { capabilityId, clusterId = cluster.Id, includePrivate = true }) ?? "",
                        Rel = "related",
                        Allow = { Get }
                    },
                    RequestAccess =
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(CapabilityController.RequestKafkaClusterAccess),
                            controller: GetNameOf<CapabilityController>(),
                            values: new { id = capabilityId, clusterId = cluster.Id }) ?? "",
                        Rel = "self",
                        Allow = requestAccessAllow
                    },
                    CreateTopic =
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(CapabilityController.AddCapabilityTopic),
                            controller: GetNameOf<CapabilityController>(),
                            values: new { id = capabilityId }) ?? "",
                        Rel = "self",
                        Allow = createTopicAllow
                    },
                }
            });
        }

        var resource = new KafkaClusterAccessListApiResource
        {
            Items = items.ToArray(),
            Links =
            {
                Self =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetKafkaClusterAccessList),
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capabilityId }) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
        return resource;
    }

    public async Task<MembershipApplicationListApiResource> Convert(CapabilityId capabilityId,
        IEnumerable<MembershipApplication> applications, UserId userId)
    {
        var allowedInteractions = Allow.Get;
        if (await _authorizationService.CanApply(CurrentUser, capabilityId))
        {
            allowedInteractions += Post;
        }

        var resource = new MembershipApplicationListApiResource
        {
            Items = applications
                .Select(application => Convert(application, userId))
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                               httpContext: HttpContext,
                               controller: GetNameOf<CapabilityController>(),
                               action: nameof(CapabilityController.GetCapabilityMembershipApplications),
                               values: new { id = capabilityId }) ??
                           "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        };
        return resource;
    }

    public async Task<KafkaTopicListApiResource> Convert(IEnumerable<KafkaTopic> topics,
        IEnumerable<KafkaCluster> clusters, KafkaTopicQueryParams queryParams)
    {
        var list = new List<KafkaTopicApiResource>();
        foreach (var topic in topics)
        {
            var apiResource = await Convert(topic);
            list.Add(apiResource);
        }

        return new KafkaTopicListApiResource
        {
            Items = list.ToArray(),
            Embedded =
            {
                KafkaClusters = Convert(clusters)
            },
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<KafkaTopicController>(),
                        action: nameof(KafkaTopicController.GetAllTopics),
                        values: queryParams) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    public MyProfileApiResource Convert(UserId userId, IEnumerable<Capability> capabilities, Member? member,
        bool isDevelopment)
    {
        return new MyProfileApiResource
        {
            Id = userId,
            Capabilities = capabilities.Select(ConvertToListItem),
            AutoReloadTopics = !isDevelopment,
            PersonalInformation = member != null
                ? new PersonalInformationApiResource
                {
                    Name = member.DisplayName ?? "",
                    Email = member.Email
                }
                : PersonalInformationApiResource.Empty,
            Links =
            {
                Self =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<MeController>(),
                        action: nameof(MeController.GetMe)) ?? "",
                    Rel = "self",
                    Allow = { Get }
                },
                PersonalInformation =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<MeController>(),
                        action: nameof(MeController.UpdatePersonalInformation)) ?? "",
                    Rel = "related",
                    Allow = { Put }
                },
                PortalVisits =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<PortalVisitController>(),
                        action: nameof(PortalVisitController.RegisterVisit)) ?? "",
                    Rel = "related",
                    Allow = { Post }
                },
                TopVisitors =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        controller: GetNameOf<SystemController>(),
                        action: nameof(SystemController.GetTopVisitors)) ?? "",
                    Rel = "related",
                    Allow = { Get }
                }
            }
        };
    }
}
