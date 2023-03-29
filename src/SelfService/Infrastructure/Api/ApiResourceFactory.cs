using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.Kafka;
using SelfService.Infrastructure.Api.MembershipApplications;

namespace SelfService.Infrastructure.Api;

public class ApiResourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public ApiResourceFactory(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new ApplicationException("Not in a http request context!");

    /// <summary>
    /// This returns a name for a controller that complies with the naming convention in ASP.NET where
    /// the "Controller" suffix should be omitted.
    /// </summary>
    /// <typeparam name="TController">The controller to extract the name from.</typeparam>
    /// <returns>Name on controller that adheres to the default naming convention (e.g. "FooController" -> "Foo").</returns>
    private static string GetNameOf<TController>()  where TController : ControllerBase 
        => typeof(TController).Name.Replace("Controller", "");

    public KafkaTopicApiResource Convert(KafkaTopic topic, UserAccessLevelOptions accessLevel)
    {
        var messageContractsAccessLevel = Convert(accessLevel);
        if (accessLevel == UserAccessLevelOptions.Read && topic.IsPrivate)
        {
            // remove all access if the topic is private and user just as read access
            messageContractsAccessLevel.Clear();
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
                    Allow = {"GET"}
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
                }
            }
        };

        return result;
    }

    private static List<string> Convert(UserAccessLevelOptions accessLevel)
    {
        return accessLevel switch
        {
            UserAccessLevelOptions.ReadWrite => new List<string> {"GET", "POST"},
            _ => new List<string> {"GET"},
        };
    }

    public MemberApiResource Convert(Member member)
    {
        return new MemberApiResource
        {
            Id = member.Id.ToString(),
            Name = member.DisplayName,
            Email = member.Email,

            // Note: [jandr] current design does not include the need for links
        };
    }

    public CapabilityDetailsApiResource Convert(Capability capability, UserAccessLevelOptions accessLevel)
    {
        var allowedTopicInteractions = new List<string> {"GET"};
        if (accessLevel == UserAccessLevelOptions.ReadWrite)
        {
            allowedTopicInteractions.Add("POST");
        }

        var allowedMembershipApplicationInteractions = new List<string> { "GET" };
        if (accessLevel == UserAccessLevelOptions.Read)
        {
            allowedMembershipApplicationInteractions.Add("POST");
        }

        return new CapabilityDetailsApiResource
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
                    Allow = {"GET"}
                },
                Members = 
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityMembers),
                        controller: GetNameOf<CapabilityController>(),
                        values: new {id = capability.Id}) ?? "",
                    Rel = "related",
                    Allow = {"GET"}
                },
                Topics = 
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityTopics),
                        controller: GetNameOf<CapabilityController>(),
                        values: new {id = capability.Id}) ?? "",
                    Rel = "related",
                    Allow = allowedTopicInteractions
                },
                MembershipApplications =
                {
                    Href = _linkGenerator.GetUriByAction(
                        httpContext: HttpContext,
                        action: nameof(CapabilityController.GetCapabilityMembershipApplications),
                        controller: GetNameOf<CapabilityController>(),
                        values: new {id = capability.Id}) ?? "",
                    Rel = "related",
                    Allow = allowedMembershipApplicationInteractions
                }
            },
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
            Allow = {"GET"}
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
                    Allow = {"GET"}
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
                    Allow = {"GET"}
                }
            }
        };
    }

    public MessageContractListApiResource Convert(IEnumerable<MessageContract> contracts, 
        KafkaTopicId kafkaTopicId, UserAccessLevelOptions accessLevel)
    {
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
                        values: new {id = kafkaTopicId}) ?? "",
                    Rel = "self",
                    Allow = Convert(accessLevel)
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

        var allowedApprovalInteractions = new List<string>();
        if (!isCurrentUserTheApplicant)
        {
            allowedApprovalInteractions.Add("GET");
            if (!application.HasApproved(currentUser))
            {
                allowedApprovalInteractions.Add("POST");
            }
        }

        return new MembershipApplicationApiResource
        {
            Id = application.Id.ToString(),
            Applicant = application.Applicant,
            SubmittedAt = application.SubmittedAt.ToUniversalTime().ToString("O"),
            DeadlineAt = application.ExpiresOn.ToUniversalTime().ToString("O"),
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
                    Allow = {"GET"}
                }
            }
        };
    }

    public MembershipApprovalListApiResource Convert(IEnumerable<MembershipApproval> approvals, MembershipApplicationId parentApplicationId, List<string> allowedInteractions)
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

    public MembershipApprovalApiResource Convert(MembershipApproval approval)
    {
        return new MembershipApprovalApiResource
        {
            Id = approval.Id.ToString("N"),
            ApprovedBy = approval.ApprovedBy,
            ApprovedAt = approval.ApprovedAt.ToUniversalTime().ToString("O")
        };
    }
}