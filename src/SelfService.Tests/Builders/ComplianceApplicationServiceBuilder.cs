using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ComplianceApplicationServiceBuilder
{
    private ICapabilityRepository _capabilityRepository;
    private IAwsAccountRepository _awsAccountRepository;
    private IMembershipRepository _membershipRepository;
    private RequirementsDbContext? _requirementsDbContext;
    private IComplianceSnapshotRepository _complianceSnapshotRepository;
    private SystemTime _systemTime;

    public ComplianceApplicationServiceBuilder()
    {
        _capabilityRepository = Dummy.Of<ICapabilityRepository>();
        _awsAccountRepository = DefaultAwsAccountRepository();
        _membershipRepository = DefaultMembershipRepository();
        _complianceSnapshotRepository = Dummy.Of<IComplianceSnapshotRepository>();
        _systemTime = SystemTime.Default;
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

    public ComplianceApplicationServiceBuilder WithMembershipRepository(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
        return this;
    }

    public ComplianceApplicationServiceBuilder WithComplianceSnapshotRepository(
        IComplianceSnapshotRepository complianceSnapshotRepository
    )
    {
        _complianceSnapshotRepository = complianceSnapshotRepository;
        return this;
    }

    public ComplianceApplicationServiceBuilder WithSystemTime(SystemTime systemTime)
    {
        _systemTime = systemTime;
        return this;
    }

    public IComplianceApplicationService Build()
    {
        if (_requirementsDbContext != null)
        {
            return new ComplianceApplicationService(
                _capabilityRepository,
                _awsAccountRepository,
                _membershipRepository,
                _requirementsDbContext,
                _complianceSnapshotRepository,
                _systemTime
            );
        }

        return new StubComplianceApplicationService(
            _capabilityRepository,
            _awsAccountRepository,
            _membershipRepository,
            _systemTime
        );
    }

    private static IAwsAccountRepository DefaultAwsAccountRepository()
    {
        var mock = new Mock<IAwsAccountRepository>();
        mock.Setup(r => r.FindBy(It.IsAny<CapabilityId>())).ReturnsAsync((AwsAccount?)null);
        mock.Setup(r => r.GetByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new List<AwsAccount>());
        return mock.Object;
    }

    private static IMembershipRepository DefaultMembershipRepository()
    {
        var mock = new Mock<IMembershipRepository>();
        mock.Setup(r => r.GetMemberCountsByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new Dictionary<CapabilityId, int>());
        return mock.Object;
    }
}
