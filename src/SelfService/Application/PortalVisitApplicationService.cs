using SelfService.Domain;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class PortalVisitApplicationService : IPortalVisitApplicationService
{
    private readonly ILogger<PortalVisitApplicationService> _logger;
    private readonly IPortalVisitRepository _portalVisitRepository;
    private readonly SystemTime _systemTime;

    public PortalVisitApplicationService(ILogger<PortalVisitApplicationService> logger, IPortalVisitRepository portalVisitRepository, 
        SystemTime systemTime)
    {
        _logger = logger;
        _portalVisitRepository = portalVisitRepository;
        _systemTime = systemTime;
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterVisit(UserId userId)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}",
            nameof(RegisterVisit), GetType().FullName);

        await _portalVisitRepository.Add(PortalVisit.Register(
            visitedBy: userId,
            visitedAt: _systemTime.Now
        ));

        _logger.LogDebug("Registered a portal visit for user {UserId}", userId);
    }
}