using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.Kafka;

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

    public KafkaTopicDto Convert(KafkaTopic topic, UserAccessLevelOptions accessLevel)
    {
        var result = new KafkaTopicDto
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
                {
                    "self", new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaTopicController.GetTopic),
                            controller: GetNameOf<KafkaTopicController>(),
                            values: new {id = topic.Id}) ?? "",
                        Rel = "self",
                        Allow = {"GET"}
                    }
                },
            }
        };

        if (topic.IsPublic)
        {
            result.Links.Add("messageContracts", new ResourceLink
            {
                Href = _linkGenerator.GetUriByAction(
                    httpContext: HttpContext,
                    action: nameof(KafkaTopicController.GetMessageContracts),
                    controller: GetNameOf<KafkaTopicController>(),
                    values: new {id = topic.Id}) ?? "?",
                Rel = "related",
                Allow = Convert(accessLevel)
            });
        }

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

    public MemberDto Convert(Member member)
    {
        return new MemberDto
        {
            Upn = member.Id.ToString(), // NOTE: [jandr] consider renaming upn to id in api contract
            Name = member.DisplayName,
            Email = member.Email,

            // Note: [jandr] current design does not include the need for links
        };
    }

    public CapabilityApiResource Convert(Capability capability, UserAccessLevelOptions accessLevel)
    {
        var allowedInteractions = new List<string> {"GET"};
        if (accessLevel == UserAccessLevelOptions.ReadWrite)
        {
            allowedInteractions.Add("POST");
        }

        return new CapabilityApiResource
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
                    Allow = allowedInteractions
                }
            },
        };
    }

    public KafkaClusterDto Convert(KafkaCluster cluster)
    {
        return new KafkaClusterDto
        {
            Id = cluster.Id.ToString(),
            Name = cluster.Name,
            Description = cluster.Description,
            Links =
            {
                {
                    "self", new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaClusterController.GetById),
                            controller: GetNameOf<KafkaClusterController>(),
                            values: new {id = cluster.Id}) ?? "",
                        Rel = "self",
                        Allow = {"GET"}
                    }
                }
            }
        };
    }

    public MessageContractDto Convert(MessageContract messageContract)
    {
        return new MessageContractDto
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
                {
                    "self", new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext, 
                            action: nameof(KafkaTopicController.GetSingleMessageContract), 
                            controller: GetNameOf<KafkaTopicController>(),
                            values: new { id = messageContract.KafkaTopicId, contractId = messageContract.Id}) ?? "",
                        Rel = "self",
                        Allow = {"GET"}
                    }
                }
            }
        };
    }

    public ResourceListDto<MessageContractDto> Convert(IEnumerable<MessageContract> contracts, 
        KafkaTopicId kafkaTopicId, UserAccessLevelOptions accessLevel)
    {
        return new ResourceListDto<MessageContractDto>
        {
            Items = contracts
                .Select(Convert)
                .ToArray(),
            Links =
            {
                {
                    "self", new ResourceLink
                    {
                        Href = _linkGenerator.GetUriByAction(
                            httpContext: HttpContext,
                            action: nameof(KafkaTopicController.GetMessageContracts),
                            controller: GetNameOf<KafkaTopicController>(),
                            values: new { id = kafkaTopicId }) ?? "",
                        Rel = "self",
                        Allow = Convert(accessLevel)
                    }
                }
            }
        };
    }

}