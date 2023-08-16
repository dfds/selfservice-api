using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Tests.Application;

public class TestAwsAccountApplicationService
{
    [Fact]
    public async Task can_generate_correct_message()
    {
        var spy = new TicketingSystemSpy();
        var capability = new CapabilityBuilder().Build();
        var awsAccount = new AwsAccountBuilder().Build();
        var sut = new AwsAccountApplicationService(
            NullLogger<AwsAccountApplicationService>.Instance,
            awsAccountRepository: new AwsAccountRepositoryStub(awsAccount),
            capabilityRepository: new CapabilityRepositoryStub(capability),
            ticketingSystem: spy,
            systemTime: new SystemTime(provider: () => DateTime.Today),
            environment: new HostingEnvironment { EnvironmentName = Environments.Production }
        );

        await sut.CreateAwsAccountRequestTicket(id: AwsAccountId.New());

        Assert.Equal(
            "*New capability context created*\n"
                + "\nRun the following command from github.com/dfds/aws-account-manifests:\n"
                + "\n```\n"
                + $"CORRELATION_ID=\"\" \\\n"
                + $"CAPABILITY_NAME=\"{capability.Name}\" \\\n"
                + $"CAPABILITY_ID=\"{capability.Id}\" \\\n"
                + $"CAPABILITY_ROOT_ID=\"{capability.Id}\" \\\n"
                + $"ACCOUNT_NAME=\"{capability.Id}\" \\\n"
                + // NB: for now account name and capability root id is the same by design
                "CONTEXT_NAME=\"default\" \\\n"
                + $"CONTEXT_ID=\"{awsAccount.Id}\" \\\n"
                + "./generate-tfvars.sh"
                + "\n```",
            spy.Message
        );
    }

    [Fact]
    public async Task send_along_headers_for_local_development()
    {
        var spy = new TicketingSystemSpy();
        var capability = new CapabilityBuilder().Build();
        var awsAccount = new AwsAccountBuilder().Build();
        var sut = new AwsAccountApplicationService(
            NullLogger<AwsAccountApplicationService>.Instance,
            awsAccountRepository: new AwsAccountRepositoryStub(awsAccount),
            capabilityRepository: new CapabilityRepositoryStub(capability),
            ticketingSystem: spy,
            systemTime: new SystemTime(provider: () => DateTime.Today),
            environment: new HostingEnvironment { EnvironmentName = Environments.Development }
        );

        await sut.CreateAwsAccountRequestTicket(id: AwsAccountId.New());

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["CORRELATION_ID"] = "",
                ["CAPABILITY_NAME"] = capability.Name,
                ["CAPABILITY_ID"] = capability.Id,
                ["CAPABILITY_ROOT_ID"] = capability.Id,
                ["ACCOUNT_NAME"] = capability.Id,
                ["CONTEXT_NAME"] = "default",
                ["CONTEXT_ID"] = awsAccount.Id,
            },
            spy.Headers
        );
    }

    [Fact]
    public async Task no_headers_are_included_in_production()
    {
        var spy = new TicketingSystemSpy();
        var capability = new CapabilityBuilder().Build();
        var awsAccount = new AwsAccountBuilder().Build();
        var sut = new AwsAccountApplicationService(
            NullLogger<AwsAccountApplicationService>.Instance,
            awsAccountRepository: new AwsAccountRepositoryStub(awsAccount),
            capabilityRepository: new CapabilityRepositoryStub(capability),
            ticketingSystem: spy,
            systemTime: new SystemTime(provider: () => DateTime.Today),
            environment: new HostingEnvironment { EnvironmentName = Environments.Production }
        );

        await sut.CreateAwsAccountRequestTicket(id: AwsAccountId.New());

        Assert.Empty(spy.Headers);
    }

    #region Test Doubles

    private class AwsAccountRepositoryStub : IAwsAccountRepository
    {
        private readonly AwsAccount? _awsAccount;

        public AwsAccountRepositoryStub(AwsAccount? awsAccount = null)
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
            return Task.FromResult(_awsAccount!);
        }

        public Task Add(AwsAccount account)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Exists(CapabilityId capabilityId)
        {
            throw new NotImplementedException();
        }
    }

    private class CapabilityRepositoryStub : ICapabilityRepository
    {
        private readonly Capability? _capability;

        public CapabilityRepositoryStub(Capability? capability = null)
        {
            _capability = capability;
        }

        public Task<Capability> Get(CapabilityId id)
        {
            return Task.FromResult(_capability!);
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

        public Task<IEnumerable<Capability>> GetAllPendingDeletion()
        {
            throw new NotImplementedException();
        }
    }

    private class TicketingSystemSpy : ITicketingSystem
    {
        public Task CreateTicket(string message, IDictionary<string, string> headers)
        {
            Message = message;
            Headers = headers;
            return Task.CompletedTask;
        }

        public string? Message { get; private set; }
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    #endregion
}
