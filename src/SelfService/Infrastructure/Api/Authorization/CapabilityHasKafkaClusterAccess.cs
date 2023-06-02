using Microsoft.AspNetCore.Authorization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Authorization;

[Obsolete]
public class CapabilityHasKafkaClusterAccess : IAuthorizationRequirement
{
}

[Obsolete]
public class CapabilityKafkaCluster
{
    public CapabilityKafkaCluster(Capability capability, KafkaCluster cluster)
    {
        Capability = capability;
        Cluster = cluster;
    }

    public Capability Capability { get; }
    public KafkaCluster Cluster { get; }
}

[Obsolete]
public class CapabilityHasKafkaClusterAccessHandler : AuthorizationHandler<CapabilityHasKafkaClusterAccess, CapabilityKafkaCluster>
{
    private readonly IKafkaClusterAccessRepository _repository;

    public CapabilityHasKafkaClusterAccessHandler(IKafkaClusterAccessRepository repository)
    {
        _repository = repository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CapabilityHasKafkaClusterAccess requirement, CapabilityKafkaCluster resource)
    {
        var clusterAccess = await _repository.FindBy(resource.Capability.Id, resource.Cluster.Id);
        if (clusterAccess != null && clusterAccess.IsAccessGranted)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this, $"Capability {resource.Capability.Id} does not have access to cluster {resource.Cluster.Name}"));
        }
    }
}