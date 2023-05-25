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
    private readonly IAwsAccountApplicationService _awsAccountApplicationService;

    public K8sNamespaceCreatedAndAwsArnConnectedHandler(IAwsAccountApplicationService awsAccountApplicationService)
    {
        _awsAccountApplicationService = awsAccountApplicationService;
    }

    public Task Handle(K8sNamespaceCreatedAndAwsArnConnected message, MessageHandlerContext context)
    {
        if (!AwsAccountId.TryParse(message.ContextId, out var id))
        {
            throw new InvalidOperationException($"Invalid AwsAccountId {message.ContextId}");
        }

        return _awsAccountApplicationService.LinkKubernetesNamespace(id, message.NamespaceName);
    }
}