using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class MembershipApplicationServiceBuilder
{
    private IMembershipRepository _membershipRepository;

    public MembershipApplicationServiceBuilder()
    {
        _membershipRepository = Dummy.Of<IMembershipRepository>();
    }

    public MembershipApplicationServiceBuilder WithMembershipRepository(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
        return this;
    }

    public MembershipApplicationService Build()
    {
        return new MembershipApplicationService(
            logger: NullLogger<MembershipApplicationService>.Instance,
            capabilityRepository: Mock.Of<ICapabilityRepository>(),
            membershipRepository: _membershipRepository,
            membershipApplicationRepository: Mock.Of<IMembershipApplicationRepository>(),
            authorizationService: Mock.Of<IAuthorizationService>(),
            systemTime: SystemTime.Default,
            membershipQuery: Mock.Of<IMembershipQuery>(),
            membershipApplicationDomainService: Mock.Of<IMembershipApplicationDomainService>()
        );
    }

    public static implicit operator MembershipApplicationService(MembershipApplicationServiceBuilder builder) =>
        builder.Build();
}
