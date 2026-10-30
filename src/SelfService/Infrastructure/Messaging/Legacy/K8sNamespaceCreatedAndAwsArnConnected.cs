using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Messaging.Legacy;

public class K8sNamespaceCreatedAndAwsArnConnected
{
    public const string EventType = "k8s_namespace_created_and_aws_arn_connected";

    public string? CapabilityId { get; set; }
    public string? ContextId { get; set; }
    public string? NamespaceName { get; set; }
}

public class K8sNamespaceCreatedAndAwsArnConnectedHandler : IMessageHandler<K8sNamespaceCreatedAndAwsArnConnected>
{
    private readonly IKubernetesAccessApplicationService _kubernetesAccessApplicationService;

    public K8sNamespaceCreatedAndAwsArnConnectedHandler(
        IKubernetesAccessApplicationService kubernetesAccessApplicationService
    )
    {
        _kubernetesAccessApplicationService = kubernetesAccessApplicationService;
    }

    public Task Handle(K8sNamespaceCreatedAndAwsArnConnected message, MessageHandlerContext context)
    {
        if (!AwsAccountId.TryParse(message.ContextId, out var awsAccountId))
        {
            throw new InvalidOperationException($"Invalid AwsAccountId {message.ContextId}");
        }

        if (message.NamespaceName is null)
        {
            throw new InvalidOperationException("NamespaceName is required");
        }

        return _kubernetesAccessApplicationService.GrantKubernetesAccess(awsAccountId, message.NamespaceName);
    }
}
