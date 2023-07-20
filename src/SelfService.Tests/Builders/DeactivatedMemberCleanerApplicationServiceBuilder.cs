using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;
using Microsoft.Extensions.Logging; //for our own homemade things
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Application;

namespace SelfService.Tests.Builders;

public class DeactivatedMemberCleanerApplicationServiceBuilder
{
    public NullLogger<SelfService.Application.DeactivatedMemberCleanerApplicationService> logger;
    private SystemTime _systemTime;
    private IMembershipRepository _membershipRepository ;
    private IMemberRepository _memberRepository;
    private IMembershipApplicationRepository _membershipApplicationRepository;
    //private SelfServiceDbContext _dbContext;
    // add fields we need, logger, dbContext, userStatusChecker, etc
    private ILogger<DeactivatedMemberCleanerApplicationService> _logger; //make correct logger
    public DeactivatedMemberCleanerApplicationServiceBuilder()
    {

        _membershipRepository = Dummy.Of<IMembershipRepository>();
        _memberRepository = Dummy.Of<IMemberRepository>();
        _membershipApplicationRepository = Dummy.Of<IMembershipApplicationRepository>();
        _logger = Dummy.Of<ILogger<DeactivatedMemberCleanerApplicationService>>();
        _systemTime = SystemTime.Default;
        //_dbContext = Dummy.OfWithParameters<SelfServiceDbContext>();
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMembershipRepository(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMemberRepository(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithSystemTime(DateTime systemTime)
    {
        _systemTime = new SystemTime(() => systemTime);
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMembershipApplicationRepository(IMembershipApplicationRepository membershipApplicationRepository)
    {
        _membershipApplicationRepository = membershipApplicationRepository;
        return this;
    }

    public SelfService.Application.DeactivatedMemberCleanerApplicationService Build()
    {
        return new SelfService.Application.DeactivatedMemberCleanerApplicationService(
            logger: _logger,
            membershipRepository: _membershipRepository,
            memberRepository: _memberRepository,
            membershipApplicationRepository: _membershipApplicationRepository
        );
    }

    public static implicit operator SelfService.Application.DeactivatedMemberCleanerApplicationService(DeactivatedMemberCleanerApplicationServiceBuilder builder)
       => builder.Build();
}