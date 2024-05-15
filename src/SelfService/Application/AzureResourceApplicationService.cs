using Microsoft.Extensions.Localization;
using SelfService.Domain;
using SelfService.Domain.Events;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Application;

public class AzureResourceApplicationService : IAzureResourceApplicationService
{
    private readonly ILogger<AzureResourceApplicationService> _logger;
    private readonly IAzureResourceRepository _azureResourceRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly SystemTime _systemTime;
    private readonly IHostEnvironment _environment;

    public AzureResourceApplicationService(
        ILogger<AzureResourceApplicationService> logger,
        IAzureResourceRepository azureResourceRepository,
        ICapabilityRepository capabilityRepository,
        SystemTime systemTime,
        IHostEnvironment environment
    )
    {
        _logger = logger;
        _azureResourceRepository = azureResourceRepository;
        _capabilityRepository = capabilityRepository;
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

        var resource = AzureResource.RequestNew(capabilityId, environment, _systemTime.Now, requestedBy);

        await _azureResourceRepository.Add(resource);

        return resource.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task PublishResourceManifestToGit(AzureResourceRequested azureResourceRequested)
    {
        var resource = await _azureResourceRepository.Get(azureResourceRequested.AzureResourceId!);
        var capability = await _capabilityRepository.Get(resource.CapabilityId);
    }
}
