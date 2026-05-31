using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ComplianceApplicationServiceBuilder
{
    private ICapabilityRepository _capabilityRepository;
    private IAwsAccountRepository _awsAccountRepository;
    private RequirementsDbContext? _requirementsDbContext;

    public ComplianceApplicationServiceBuilder()
    {
        _capabilityRepository = Dummy.Of<ICapabilityRepository>();
        _awsAccountRepository = DefaultAwsAccountRepository();
    }

    public ComplianceApplicationServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public ComplianceApplicationServiceBuilder WithAwsAccountRepository(IAwsAccountRepository awsAccountRepository)
    {
        _awsAccountRepository = awsAccountRepository;
        return this;
    }

    public ComplianceApplicationServiceBuilder WithRequirementsDbContext(RequirementsDbContext requirementsDbContext)
    {
        _requirementsDbContext = requirementsDbContext;
        return this;
    }

    public IComplianceApplicationService Build()
    {
        if (_requirementsDbContext != null)
        {
            return new ComplianceApplicationService(
                _capabilityRepository,
                _awsAccountRepository,
                _requirementsDbContext
            );
        }

        return new StubComplianceApplicationService(_capabilityRepository, _awsAccountRepository);
    }

    private static IAwsAccountRepository DefaultAwsAccountRepository()
    {
        var mock = new Mock<IAwsAccountRepository>();
        mock.Setup(r => r.FindBy(It.IsAny<CapabilityId>())).ReturnsAsync((AwsAccount?)null);
        mock.Setup(r => r.GetByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new List<AwsAccount>());
        return mock.Object;
    }
}
