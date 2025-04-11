using Microsoft.Extensions.Logging;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class InvitationApplicationServiceBuilder
{
    private IInvitationRepository _invitationRepository = Dummy.Of<IInvitationRepository>();
    private ICapabilityRepository _capabilityRepository = Dummy.Of<ICapabilityRepository>();
    private ILogger<InvitationApplicationService> _logger = Dummy.Of<ILogger<InvitationApplicationService>>();
    private IMembershipRepository _membershipRepository = Dummy.Of<IMembershipRepository>();
    private IMembershipApplicationRepository _membershipApplicationRepository =
        Dummy.Of<IMembershipApplicationRepository>();

    public InvitationApplicationServiceBuilder WithDbContextAndDefaultRepositories(SelfServiceDbContext dbContext)
    {
        _invitationRepository = new InvitationRepository(dbContext, SystemTime.Default);
        _capabilityRepository = new CapabilityRepository(dbContext);
        _membershipRepository = new MembershipRepository(dbContext);
        _membershipApplicationRepository = new MembershipApplicationRepository(dbContext, SystemTime.Default);
        return this;
    }

    public InvitationApplicationServiceBuilder WithInvitationRepository(IInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
        return this;
    }

    public InvitationApplicationServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public InvitationApplicationServiceBuilder WithMembershipRepository(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
        return this;
    }

    public InvitationApplicationServiceBuilder WithMembershipApplicationRepository(
        IMembershipApplicationRepository membershipApplicationRepository
    )
    {
        _membershipApplicationRepository = membershipApplicationRepository;
        return this;
    }

    public InvitationApplicationServiceBuilder WithLogger(ILogger<InvitationApplicationService> logger)
    {
        _logger = logger;
        return this;
    }

    public InvitationApplicationService Build()
    {
        return new(
            _invitationRepository,
            _capabilityRepository,
            _membershipRepository,
            _membershipApplicationRepository,
            _logger
        );
    }
}
