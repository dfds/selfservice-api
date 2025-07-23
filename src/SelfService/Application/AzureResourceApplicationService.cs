using System.Text.Json.Nodes;
using SelfService.Domain;
using SelfService.Domain.Events;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class AzureResourceApplicationService : IAzureResourceApplicationService
{
    private readonly ILogger<AzureResourceApplicationService> _logger;
    private readonly IAzureResourceRepository _azureResourceRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly SystemTime _systemTime;
    private readonly IHostEnvironment _environment;

    public AzureResourceApplicationService(
        ILogger<AzureResourceApplicationService> logger,
        IAzureResourceRepository azureResourceRepository,
        ICapabilityRepository capabilityRepository,
        IServiceScopeFactory serviceScopeFactory,
        SystemTime systemTime,
        IHostEnvironment environment
    )
    {
        _logger = logger;
        _azureResourceRepository = azureResourceRepository;
        _capabilityRepository = capabilityRepository;
        _serviceScopeFactory = serviceScopeFactory;
        _systemTime = systemTime;
        _environment = environment;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<AzureResourceId> RequestAzureResource(
        CapabilityId capabilityId,
        string environment,
        UserId requestedBy
    )
    {
        if (await _azureResourceRepository.Exists(capabilityId, environment))
        {
            throw new AlreadyHasAzureResourceException(
                $"Capability {capabilityId} already has Azure Resource for environment '{environment}'"
            );
        }

        var capability = await _capabilityRepository.Get(capabilityId);
        var jsonObject = JsonNode.Parse(capability!.JsonMetadata)?.AsObject()!;
        var mandatoryTags = new List<String> { "dfds.owner", "dfds.cost.centre", "dfds.service.availability" };
        foreach (var tag in mandatoryTags)
        {
            if (!jsonObject.ContainsKey(tag))
            {
                throw new MissingMandatoryJsonMetadataException(
                    $"Capability {capabilityId} does not have required tag '{tag}' for Azure Resource creation"
                );
            }
        }

        var resource = AzureResource.RequestNew(capabilityId, environment, _systemTime.Now, requestedBy);

        await _azureResourceRepository.Add(resource);

        return resource.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task PublishResourceManifestToGit(AzureResourceRequested azureResourceRequested)
    {
        var resource = await _azureResourceRepository.Get(azureResourceRequested.AzureResourceId!);
        var capability = await _capabilityRepository.Get(resource.CapabilityId);
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var azureResourceManifestRepository = scope.ServiceProvider.GetService<IAzureResourceManifestRepository>();
            await azureResourceManifestRepository!.Add(
                new AzureResourceManifest { AzureResource = resource, Capability = capability }
            );
        }
    }
}
