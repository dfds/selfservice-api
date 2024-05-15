using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IAzureResourceApplicationService
{
    Task<AzureResourceId> RequestAzureResource(CapabilityId capabilityId, string environment, UserId requestedBy);
    public Task PublishResourceManifestToGit(AzureResourceRequested azureResourceRequested);
}
