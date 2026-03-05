using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ComplianceApplicationServiceBuilder
{
    private ICapabilityRepository _capabilityRepository;
    private RequirementsDbContext? _requirementsDbContext;

    public ComplianceApplicationServiceBuilder()
    {
        _capabilityRepository = Dummy.Of<ICapabilityRepository>();
    }

    public ComplianceApplicationServiceBuilder WithCapabilityRepository(
        ICapabilityRepository capabilityRepository
    )
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public ComplianceApplicationServiceBuilder WithRequirementsDbContext(
        RequirementsDbContext requirementsDbContext
    )
    {
        _requirementsDbContext = requirementsDbContext;
        return this;
    }

    public IComplianceApplicationService Build()
    {
        if (_requirementsDbContext != null)
        {
            return new ComplianceApplicationService(_capabilityRepository, _requirementsDbContext);
        }

        return new StubComplianceApplicationService(_capabilityRepository);
    }
}
