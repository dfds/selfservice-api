using SelfService.Configuration;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Messaging;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Queries;
using SelfService.Tests.Infrastructure.Api;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ApiApplicationBuilder
{
    private IAwsAccountRepository _awsAccountRepository;
    private ICapabilityRepository _capabilityRepository;
    private IReleaseNoteRepository _releaseNoteRepository;
    private IMembershipQuery _membershipQuery;
    private ICapabilityDeletionStatusQuery _capabilityDeletionStatusQuery;

    public ApiApplicationBuilder()
    {
        _awsAccountRepository = new StubAwsAccountRepository();
        _capabilityRepository = new StubCapabilityRepository();
        _releaseNoteRepository = new StubReleaseNoteRepository();
        _membershipQuery = new StubMembershipQuery();
        _capabilityDeletionStatusQuery = new StubCapabilityDeletionStatusQuery();
    }

    public ApiApplicationBuilder WithAwsAccountRepository(IAwsAccountRepository awsAccountRepository)
    {
        _awsAccountRepository = awsAccountRepository;
        return this;
    }

    public ApiApplicationBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public ApiApplicationBuilder WithMembershipQuery(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
        return this;
    }

    public ApiApplicationBuilder WithCapabilityDeletionStatusQuery(
        ICapabilityDeletionStatusQuery capabilityDeletionStatusQuery
    )
    {
        _capabilityDeletionStatusQuery = capabilityDeletionStatusQuery;
        return this;
    }

    public ApiApplicationBuilder WithReleaseNoteRepository(IReleaseNoteRepository releaseNoteRepository)
    {
        _releaseNoteRepository = releaseNoteRepository;
        return this;
    }

    public ApiApplication Build()
    {
        var application = new ApiApplication();
        application.ReplaceService<IAwsAccountRepository>(_awsAccountRepository);
        application.ReplaceService<ICapabilityRepository>(_capabilityRepository);
        application.ReplaceService<IReleaseNoteRepository>(_releaseNoteRepository);
        application.ReplaceService<IMembershipQuery>(_membershipQuery);
        application.ReplaceService<ICapabilityDeletionStatusQuery>(_capabilityDeletionStatusQuery);
        application.ReplaceService<IMessagingService>(new StubMessagingService());
        return application;
    }
}
