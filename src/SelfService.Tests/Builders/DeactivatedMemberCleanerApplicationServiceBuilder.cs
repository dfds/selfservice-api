using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class DeactivatedMemberCleanerApplicationServiceBuilder
{
    private IMembershipRepository _membershipRepository;
    private IMemberRepository _memberRepository;
    private IMembershipApplicationRepository _membershipApplicationRepository;
    private ILogger<DeactivatedMemberCleanerApplicationService> _logger; //make correct logger

    public DeactivatedMemberCleanerApplicationServiceBuilder()
    {
        _membershipRepository = Dummy.Of<IMembershipRepository>();
        _memberRepository = Dummy.Of<IMemberRepository>();
        _membershipApplicationRepository = Dummy.Of<IMembershipApplicationRepository>();
        _logger = Dummy.Of<ILogger<DeactivatedMemberCleanerApplicationService>>();
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMembershipRepository(
        IMembershipRepository membershipRepository
    )
    {
        _membershipRepository = membershipRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMemberRepository(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMembershipApplicationRepository(
        IMembershipApplicationRepository membershipApplicationRepository
    )
    {
        _membershipApplicationRepository = membershipApplicationRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationService Build()
    {
        return new DeactivatedMemberCleanerApplicationService(
            logger: _logger,
            membershipRepository: _membershipRepository,
            memberRepository: _memberRepository,
            membershipApplicationRepository: _membershipApplicationRepository
        );
    }

    public static implicit operator DeactivatedMemberCleanerApplicationService(
        DeactivatedMemberCleanerApplicationServiceBuilder builder
    ) => builder.Build();
}
