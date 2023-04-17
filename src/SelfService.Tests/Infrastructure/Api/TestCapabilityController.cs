using System.Net;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using Xunit.Abstractions;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityController
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestCapabilityController(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task getting_non_existing_capability_by_id_returns_not_found()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub());
        
        using var client = application.CreateClient();

        var response = await client.GetAsync("/capabilities/some-capability");

        Assert.Equal(
            expected: HttpStatusCode.NotFound,
            actual: response.StatusCode
        );
    }

    [Fact]
    public async Task getting_capability_by_id_returns_ok()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new CapabilityRepositoryStub(new CapabilityBuilder().Build()));
        application.ReplaceService<IAwsAccountRepository>(new AwsAccountRepositoryStub());
        application.ReplaceService<IAuthorizationService>(new AuthorizationServiceStub());
        
        using var client = application.CreateClient();

        var response = await client.GetAsync("/capabilities/some-capability");

        var s = await response.Content.ReadAsStringAsync();
        
        _testOutputHelper.WriteLine(s);
        
        Assert.Equal(
            expected: HttpStatusCode.OK,
            actual: response.StatusCode
        );
    }

    [Obsolete]
    public class AuthorizationServiceStub : IAuthorizationService
    {
        private readonly UserAccessLevelOptions _userAccessLevelOptions;

        public AuthorizationServiceStub(UserAccessLevelOptions userAccessLevelOptions = UserAccessLevelOptions.Read)
        {
            _userAccessLevelOptions = userAccessLevelOptions;
        }

        public Task<UserAccessLevelOptions> GetUserAccessLevelForCapability(UserId userId, CapabilityId capabilityId)
        {
            return Task.FromResult(_userAccessLevelOptions);
        }
    }
    
    public class AwsAccountRepositoryStub : IAwsAccountRepository
    {
        private readonly AwsAccount? _awsAccount;

        public AwsAccountRepositoryStub(AwsAccount? awsAccount=null)
        {
            _awsAccount = awsAccount;
        }

        public Task<AwsAccount?> FindBy(CapabilityId capabilityId)
        {
            return Task.FromResult(_awsAccount);
        }

        public Task<List<AwsAccount>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<AwsAccount> Get(AwsAccountId id)
        {
            throw new NotImplementedException();
        }

        public Task Add(AwsAccount account)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Exists(CapabilityId capabilityId)
        {
            return Task.FromResult(_awsAccount != null);
        }
    }
    
    public class CapabilityRepositoryStub : ICapabilityRepository
    {
        private readonly Capability? _capability;

        public CapabilityRepositoryStub(Capability? capability = null)
        {
            _capability = capability;
        }

        public Task<Capability> Get(CapabilityId id)
        {
            throw new NotImplementedException();
        }

        public Task<Capability?> FindBy(CapabilityId id)
        {
            return Task.FromResult(_capability);
        }

        public Task<bool> Exists(CapabilityId id)
        {
            throw new NotImplementedException();
        }

        public Task Add(Capability capability)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Capability>> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}