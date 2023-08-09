using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Me;

[Route("stats")]
[Produces("application/json")]
[ApiController]
public class StatsController : ControllerBase
{
    private readonly IMyCapabilitiesQuery _myCapabilitiesQuery;
    private readonly SelfServiceDbContext _dbContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberApplicationService _memberApplicationService;

    public StatsController(
        IMyCapabilitiesQuery myCapabilitiesQuery,
        SelfServiceDbContext dbContext,
        IHostEnvironment hostEnvironment,
        ApiResourceFactory apiResourceFactory,
        IMemberRepository memberRepository,
        IMemberApplicationService memberApplicationService
    )
    { //this is called implicitly by some black magic in Program.cs I think
        _myCapabilitiesQuery = myCapabilitiesQuery;
        _dbContext = dbContext;
        _hostEnvironment = hostEnvironment;
        _apiResourceFactory = apiResourceFactory;
        _memberRepository = memberRepository;
        _memberApplicationService = memberApplicationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetStats()
    {
        return Ok(await ComposeStats());
    }

    private async Task<Stat[]> ComposeStats()
    {
        //first fetch/compute the values:
        int capabilitiesStat = await _dbContext.Capabilities
            .Where(x => x.Status != CapabilityStatusOptions.Deleted)
            .CountAsync();

        int awsAccountsStat = await _dbContext.AwsAccounts.CountAsync();

        int kubernetesClustersStat = 1; //don't ask

        int kafkaClustersStat = await _dbContext.KafkaClusters.Where(x => x.Enabled).CountAsync();

        int publicTopicsStat = await _dbContext.KafkaTopics
            .Where(x => ((string)x.Name).StartsWith("pub."))
            .CountAsync();

        int privateTopicsStat = await _dbContext.KafkaTopics
            .Where(x => !((string)x.Name).StartsWith("pub."))
            .CountAsync();

        return new Stat[]
        {
            new Stat(Title: "Capabilities", Value: capabilitiesStat),
            new Stat(Title: "AWS Accounts", Value: awsAccountsStat),
            new Stat(Title: "Kubernetes Clusters", Value: kubernetesClustersStat),
            new Stat(Title: "Kafka Clusters", Value: kafkaClustersStat),
            new Stat(Title: "Public Topics", Value: publicTopicsStat),
            new Stat(Title: "Private Topics", Value: privateTopicsStat),
        };
    }
}
