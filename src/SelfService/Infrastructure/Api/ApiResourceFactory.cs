using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Authorization;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.Kafka;
using SelfService.Infrastructure.Api.Me;
using SelfService.Infrastructure.Api.MembershipApplications;
using System.Data;
using SelfService.Infrastructure.Api.System;
using static SelfService.Infrastructure.Api.Method;

namespace SelfService.Infrastructure.Api;

public class ApiResourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IAuthorizationService _authorizationService;
    private readonly Domain.Services.IAuthorizationService _domainAuthorizationService;

    public ApiResourceFactory(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, IAuthorizationService authorizationService, Domain.Services.IAuthorizationService domainAuthorizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _authorizationService = authorizationService;
        _domainAuthorizationService = domainAuthorizationService;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new ApplicationException("Not in a http request context!");

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
    private static string GetNameOf<TController>()  where TController : ControllerBase 
        => typeof(TController).Name.Replace("Controller", "");

    public async Task<KafkaTopicApiResource> Convert(KafkaTopic topic)
    {
        var portalUser = HttpContext.User.ToPortalUser();

        var allowOnSelf = new List<string> { "GET" };
        if (await _domainAuthorizationService.CanDelete(portalUser, topic))
        {
            allowOnSelf.Add("DELETE");
        }

        var messageContractsAccessLevel = new List<string>();
        if (await _domainAuthorizationService.CanReadMessageContracts(portalUser, topic))
        {
            messageContractsAccessLevel.Add("GET");
        }

        if (await _domainAuthorizationService.CanAddMessageContract(portalUser, topic))
        {
            messageContractsAccessLevel.Add("POST");
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
            Status = topic.Status switch
            {
                KafkaTopicStatusType.Requested => "Requested",
                KafkaTopicStatusType.InProgress => "In Progress",
                KafkaTopicStatusType.Provisioned => "Provisioned",
                _ => "Unknown"
            },
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetTopic),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new {id = topic.Id}) ?? "",
                    Rel = "self",
                    Allow = allowOnSelf
                },
                MessageContracts = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(KafkaTopicController.GetMessageContracts),
                        controller: GetNameOf<KafkaTopicController>(),
                        values: new {id = topic.Id}) ?? "?",
                    Rel = "related",
                    Allow = messageContractsAccessLevel
                },
                UpdateDescription = await _domainAuthorizationService.CanChange(portalUser, topic)
                    ? new ResourceActionLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaTopicController.ChangeTopicDescription),
                            controller: GetNameOf<KafkaTopicController>(),
                            values: new {id = topic.Id}) ?? "",
                        Method = "PUT",
                }
                    : null
            }
        };

        return result;
    }

    private static Allow Convert(UserAccessLevelOptions accessLevel)
    {
        return accessLevel switch
        {
            UserAccessLevelOptions.ReadWrite => new Allow { Get, Post },
            _ => Allow.Get
        };
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
                        values: new {id = capability.Id}) ?? "",
                    Rel = "self",
                    Allow = { Get }
                },
            },
        };
    }

    private async Task<ResourceLink> CreateMembershipApplicationsLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.Get;

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            user: HttpContext.User,
            resource: capability,
            requirements: new IAuthorizationRequirement[]
            {
                new IsNotMemberOfCapability(),
                new NotHasPendingMembershipApplication(), 
            });
        
        if (authorizationResult.Succeeded)
        {
            allowedInteractions += Post;
        }

        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityMembershipApplications),
                controller: GetNameOf<CapabilityController>(),
                values: new {id = capability.Id}) ?? "",
            Rel = "related",
            Allow = allowedInteractions
        };
    }

    private async Task<ResourceLink> CreateLeaveCapabilityLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.Get;

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            user: HttpContext.User,
            resource: capability,
            requirements: new IAuthorizationRequirement[]
            {
                new IsMemberOfCapability(),
                new CapabilityHasMultipleMembers(),
            });
        
        if (authorizationResult.Succeeded)
        {
            allowedInteractions += Post;
        }

        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.LeaveCapability),
                controller: GetNameOf<CapabilityController>(),
                values: new {id = capability.Id}) ?? "",
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
                values: new {id = capability.Id}) ?? "",
            Rel = "self",
            Allow = { Get }
        };
    }

    private async Task<ResourceLink> CreateMembersLinkFor(Capability capability)
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

    private async Task<ResourceLink> CreateTopicsLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.Get;

        var postRequirements = new IAuthorizationRequirement[]
        {
            new IsMemberOfCapability()
        };

        var authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, capability, postRequirements);
        if (authorizationResult.Succeeded)
        {
            allowedInteractions += Post;
        }

        return new ResourceLink
        {
            Href = _linkGenerator.GetUriByAction(
                httpContext: HttpContext,
                action: nameof(CapabilityController.GetCapabilityTopics),
                controller: GetNameOf<CapabilityController>(),
                values: new { id = capability.Id }) ?? "",
            Rel = "related",
            Allow = allowedInteractions
        };
    }

    private async Task<ResourceLink> CreateAwsAccountLinkFor(Capability capability)
    {
        var allowedInteractions = Allow.None;

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            user: HttpContext.User,
            resource: capability,
            requirements: new[]
            {
                new IsMemberOfCapability()
            });

        if (authorizationResult.Succeeded)
        {
            allowedInteractions += Get;

            authorizationResult = await _authorizationService.AuthorizeAsync(
                user: HttpContext.User,
                resource: capability,
                requirements: new[]
                {
                    new HasNoAwsAccount()
                });

            if (authorizationResult.Succeeded)
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
                Members = await CreateMembersLinkFor(capability),
                Topics = await CreateTopicsLinkFor(capability),
                MembershipApplications = await CreateMembershipApplicationsLinkFor(capability),
                LeaveCapability = await CreateLeaveCapabilityLinkFor(capability),
                AwsAccount = await CreateAwsAccountLinkFor(capability),
            },
        };
    }

    public AwsAccountApiResource Convert(AwsAccount account, UserAccessLevelOptions accessLevel)
    {
        var allowedInteractions = Allow.Get;
        if (accessLevel == UserAccessLevelOptions.ReadWrite)
        {
            allowedInteractions += Post;
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
                        values: new {id = account.CapabilityId}) ?? "",
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
                values: new {id = cluster.Id}) ?? "",
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
                        values: new {id = messageContract.KafkaTopicId, contractId = messageContract.Id}) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    public async Task<MessageContractListApiResource> Convert(IEnumerable<MessageContract> contracts, 
        KafkaTopic parentKafkaTopic)
    {
        var allowedInteractions = new List<string> { "GET" };
        if (await _domainAuthorizationService.CanAddMessageContract(PortalUser, parentKafkaTopic))
    {
            allowedInteractions.Add("POST");
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
                        values: new {id = parentKafkaTopic.Id}) ?? "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        };
    }

    public MembershipApplicationApiResource Convert(MembershipApplication application, UserAccessLevelOptions initialAccessLevel, UserId currentUser)
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

    private MembershipApprovalListApiResource Convert(IEnumerable<MembershipApproval> approvals, MembershipApplicationId parentApplicationId, Allow allowedInteractions)
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

    public async Task<CapabilityTopicsApiResource> Convert(CapabilityTopics capabilityTopics)
    {
        var isMemberOfCapability = await IsMemberOfCapability(capabilityTopics.Capability);

        var resources = Convert(capabilityTopics, isMemberOfCapability)
            .ToBlockingEnumerable()
            .ToArray();

        var allow = Allow.Get;
        if (isMemberOfCapability)
        {
            allow += Post;
        }

        return new CapabilityTopicsApiResource
        {
            Items = resources,
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext, 
                        action: nameof(CapabilityController.GetCapabilityTopics), 
                        controller: GetNameOf<CapabilityController>(),
                        values: new { id = capabilityTopics.Capability.Id }) ?? "",
                    Rel = "self",
                    Allow = allow
                }
            }
        };
    }

    private async IAsyncEnumerable<CapabilityClusterTopicsApiResource> Convert(CapabilityTopics capabilityTopics, bool isMemberOfCapability)
    {
        foreach (var clusterTopics in capabilityTopics.Clusters)
        {
            var capabilityHasKafkaClusterAccess = await CapabilityHasKafkaClusterAccess(capabilityTopics.Capability, clusterTopics.Cluster);

            var allow = Allow.None;
            if (isMemberOfCapability)
            {
                allow += Get;
                if (!capabilityHasKafkaClusterAccess)
                {
                    allow += Post;
                }
            }

            yield return  new CapabilityClusterTopicsApiResource
            {
                Id = clusterTopics.Cluster.Id,
                Name = clusterTopics.Cluster.Name,
                Description = clusterTopics.Cluster.Description,
                Topics = clusterTopics.Topics
                    .Select(topic => Convert(topic, isMemberOfCapability ? UserAccessLevelOptions.ReadWrite : UserAccessLevelOptions.Read))
                    .ToArray(),
                Links =
                {
                    Self = new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaClusterController.GetById),
                            controller: GetNameOf<KafkaClusterController>(),
                            values: new {id = clusterTopics.Cluster.Id}) ?? "",
                        Rel = "self",
                        Allow = { Get }
                    },
                    Access = new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(CapabilityController.GetKafkaClusterAccess),
                            controller: GetNameOf<CapabilityController>(),
                            values: new {id = capabilityTopics.Capability.Id, clusterId = clusterTopics.Cluster.Id}) ?? "",
                        Rel = "self",
                        Allow = allow
                    } 
                }
            };
        }
    }

    private async Task<bool> IsMemberOfCapability(Capability capability)
    {
        var postRequirements = new IAuthorizationRequirement[]
        {
            new IsMemberOfCapability()
        };

        var authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, capability, postRequirements);
        return authorizationResult.Succeeded;
    }

    private async Task<bool> CapabilityHasKafkaClusterAccess(Capability capability, KafkaCluster kafkaCluster)
    {
        var requirements = new IAuthorizationRequirement[]
        {
            new CapabilityHasKafkaClusterAccess()
        };

        var kafkaClusterAccess = new CapabilityKafkaCluster(capability, kafkaCluster);
        var authorizationResult = await _authorizationService.AuthorizeAsync(HttpContext.User, kafkaClusterAccess, requirements);
        return authorizationResult.Succeeded;
    }

    public MembershipApplicationListApiResource Convert(string id, UserAccessLevelOptions accessLevel, IEnumerable<MembershipApplication> applications, UserId userId)
    {
        var allowedInteractions = Allow.Get;
        if (accessLevel == UserAccessLevelOptions.Read && applications.Count() == 0)
        {
            allowedInteractions += Post;
        }

        var resource = new MembershipApplicationListApiResource
        {
            Items = applications
                .Select(application => Convert(application, accessLevel, userId))
                .ToArray(),
            Links =
            {
                Self = new ResourceLink
                {
                    Href = _linkGenerator.GetUriByAction(
                               httpContext: HttpContext,
                               controller: GetNameOf<CapabilityController>(),
                               action: nameof(CapabilityController.GetCapabilityMembershipApplications),
                               values: new { id = id }) ??
                           "",
                    Rel = "self",
                    Allow = allowedInteractions
                }
            }
        };
        return resource;
    }

    public KafkaTopicListApiResource Convert(IEnumerable<KafkaTopic> topics, IEnumerable<KafkaCluster> clusters)
    {
        return new KafkaTopicListApiResource
        {
            Items = topics
                .Select(topic => Convert(topic, UserAccessLevelOptions.Read)) // NOTE [jandr@2023-03-20]: Hardcoding access level to read - change that!
                .ToArray(),
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
                        action: nameof(KafkaTopicController.GetAllTopics)) ?? "",
                    Rel = "self",
                    Allow = { Get }
                }
            }
        };
    }

    public MyProfileApiResource Convert(UserId userId, IEnumerable<Capability> capabilities, Member? member, bool isDevelopment, IEnumerable<Stat> stats)
    {

        return new MyProfileApiResource
        {
            Id = userId,
            Capabilities = capabilities.Select(ConvertToListItem), 
            Stats = stats,
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