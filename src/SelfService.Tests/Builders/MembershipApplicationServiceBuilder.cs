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
    private IMembershipApplicationRepository _membershipApplicationRepository;
    private ICapabilityRepository _capabilityRepository;
    private IAuthorizationService _authorizationService;
    private IInvitationRepository _invitationRepository;
    private IMyCapabilitiesQuery _myCapabilitiesQuery;

    public MembershipApplicationServiceBuilder()
    {
        _membershipApplicationRepository = Dummy.Of<IMembershipApplicationRepository>();
        _membershipRepository = Dummy.Of<IMembershipRepository>();
        _capabilityRepository = Dummy.Of<ICapabilityRepository>();
        _authorizationService = Dummy.Of<IAuthorizationService>();
        _invitationRepository = Dummy.Of<IInvitationRepository>();
        _myCapabilitiesQuery = Dummy.Of<IMyCapabilitiesQuery>();
    }

    public MembershipApplicationServiceBuilder WithMembershipRepository(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
        return this;
    }

    public MembershipApplicationServiceBuilder WithMembershipApplicationRepository(
        IMembershipApplicationRepository membershipApplicationRepo
    )
    {
        _membershipApplicationRepository = membershipApplicationRepo;
        return this;
    }

    public MembershipApplicationServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public MembershipApplicationServiceBuilder WithAuthorizationService(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
        return this;
    }

    public MembershipApplicationServiceBuilder WithInvitationRepository(IInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
        return this;
    }

    public MembershipApplicationService Build()
    {
        return new MembershipApplicationService(
            logger: NullLogger<MembershipApplicationService>.Instance,
            capabilityRepository: _capabilityRepository,
            membershipRepository: _membershipRepository,
            membershipApplicationRepository: _membershipApplicationRepository,
            authorizationService: _authorizationService,
            systemTime: SystemTime.Default,
            membershipQuery: Mock.Of<IMembershipQuery>(),
            membershipApplicationDomainService: Mock.Of<IMembershipApplicationDomainService>(),
            invitationRepository: _invitationRepository,
            myCapabilitiesQuery: _myCapabilitiesQuery
        );
    }

    public static implicit operator MembershipApplicationService(MembershipApplicationServiceBuilder builder) =>
        builder.Build();
}
