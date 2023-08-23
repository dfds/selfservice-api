using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Application;

public class AwsAccountApplicationService : IAwsAccountApplicationService
{
    private readonly ILogger<AwsAccountApplicationService> _logger;
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly ITicketingSystem _ticketingSystem;
    private readonly SystemTime _systemTime;
    private readonly IHostEnvironment _environment;

    public AwsAccountApplicationService(
        ILogger<AwsAccountApplicationService> logger,
        IAwsAccountRepository awsAccountRepository,
        ICapabilityRepository capabilityRepository,
        ITicketingSystem ticketingSystem,
        SystemTime systemTime,
        IHostEnvironment environment
    )
    {
        _logger = logger;
        _awsAccountRepository = awsAccountRepository;
        _capabilityRepository = capabilityRepository;
        _ticketingSystem = ticketingSystem;
        _systemTime = systemTime;
        _environment = environment;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<AwsAccountId> RequestAwsAccount(CapabilityId capabilityId, UserId requestedBy)
    {
        if (await _awsAccountRepository.Exists(capabilityId))
        {
            throw new AlreadyHasAwsAccountException($"Capability {capabilityId} already has an AWS account");
        }

        var account = AwsAccount.RequestNew(capabilityId, _systemTime.Now, requestedBy);

        await _awsAccountRepository.Add(account);

        return account.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task CreateAwsAccountRequestTicket(AwsAccountId id)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType}",
            nameof(CreateAwsAccountRequestTicket),
            GetType().FullName
        );

        var account = await _awsAccountRepository.Get(id);
        var capability = await _capabilityRepository.Get(account.CapabilityId);

        var message = CreateMessage(
            "",
            new ContextAddedToCapabilityData(
                capabilityId: capability.Id,
                capabilityName: capability.Name,
                capabilityRootId: capability.Id,
                contextId: account.Id,
                contextName: "default"
            )
        );

        var headers = new Dictionary<string, string>();

        if (_environment.IsDevelopment())
        {
            headers["TICKET_TYPE"] = TopdeskTicketType.awsAccountRequest;
            headers["CORRELATION_ID"] = "";
            headers["CAPABILITY_NAME"] = capability.Name;
            headers["CAPABILITY_ID"] = capability.Id;
            headers["CAPABILITY_ROOT_ID"] = capability.Id;
            headers["ACCOUNT_NAME"] = capability.Id;
            headers["CONTEXT_NAME"] = "default";
            headers["CONTEXT_ID"] = account.Id;
        }

        await _ticketingSystem.CreateTicket(message, headers);
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterRealAwsAccount(AwsAccountId id, RealAwsAccountId realAwsAccountId, string? roleEmail)
    {
        var account = await _awsAccountRepository.Get(id);

        account.RegisterRealAwsAccount(realAwsAccountId, roleEmail, _systemTime.Now);
    }

    [TransactionalBoundary, Outboxed]
    public async Task LinkKubernetesNamespace(AwsAccountId id, string? @namespace)
    {
        var account = await _awsAccountRepository.Get(id);

        account.LinkKubernetesNamespace(@namespace, _systemTime.Now);
    }

    private static string CreateMessage(string xCorrelationId, ContextAddedToCapabilityData payload)
    {
        var message =
            "*New capability context created*\n"
            + "\nRun the following command from github.com/dfds/aws-account-manifests:\n"
            + "\n```\n"
            + $"CORRELATION_ID=\"{xCorrelationId}\" \\\n"
            + $"CAPABILITY_NAME=\"{payload.CapabilityName}\" \\\n"
            + $"CAPABILITY_ID=\"{payload.CapabilityId}\" \\\n"
            + $"CAPABILITY_ROOT_ID=\"{payload.CapabilityRootId}\" \\\n"
            + $"ACCOUNT_NAME=\"{payload.CapabilityRootId}\" \\\n"
            + // NB: for now account name and capability root id is the same by design
            $"CONTEXT_NAME=\"{payload.ContextName}\" \\\n"
            + $"CONTEXT_ID=\"{payload.ContextId}\" \\\n"
            + "./generate-tfvars.sh"
            + "\n```";

        return message;
    }

    private class ContextAddedToCapabilityData
    {
        public string CapabilityId { get; set; }
        public string CapabilityName { get; set; }
        public string CapabilityRootId { get; set; }
        public string ContextId { get; set; }
        public string ContextName { get; set; }

        public ContextAddedToCapabilityData(
            string capabilityId,
            string capabilityName,
            string capabilityRootId,
            string contextId,
            string contextName
        )
        {
            CapabilityId = capabilityId;
            CapabilityName = capabilityName;
            CapabilityRootId = capabilityRootId;
            ContextId = contextId;
            ContextName = contextName;
        }
    }
}
