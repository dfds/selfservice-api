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

[Route("me")]
[Produces("application/json")]
[ApiController]
public class MeController : ControllerBase
{
    private readonly IMyCapabilitiesQuery _myCapabilitiesQuery;
    private readonly SelfServiceDbContext _dbContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberApplicationService _memberApplicationService;

    public MeController(
        IMyCapabilitiesQuery myCapabilitiesQuery,
        SelfServiceDbContext dbContext,
        IHostEnvironment hostEnvironment,
        ApiResourceFactory apiResourceFactory,
        IMemberRepository memberRepository,
        IMemberApplicationService memberApplicationService
    )
    {
        _myCapabilitiesQuery = myCapabilitiesQuery;
        _dbContext = dbContext;
        _hostEnvironment = hostEnvironment;
        _apiResourceFactory = apiResourceFactory;
        _memberRepository = memberRepository;
        _memberApplicationService = memberApplicationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetMe()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id."
                }
            );
        }

        var capabilities = await _myCapabilitiesQuery.FindBy(userId);
        var member = await _memberRepository.FindBy(userId);

        return Ok(_apiResourceFactory.Convert(userId, capabilities, member, _hostEnvironment.IsDevelopment()));
    }

    [HttpPut("personalinformation")]
    public async Task<IActionResult> UpdatePersonalInformation([FromBody] UpdatePersonalInformationRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id."
                }
            );
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid email",
                    Detail = $"Email \"{request.Email}\" for user \"{userId}\" is not valid."
                }
            );
        }

        await _memberApplicationService.RegisterUserProfile(userId, request.Name ?? "", request.Email);

        return NoContent();
    }

    private async Task<Stat[]> ComposeStats()
    {
        return new Stat[]
        {
            new Stat(
                Title: "Capabilities",
                Value: await _dbContext.Capabilities.Where(x => x.Deleted == null).CountAsync()
            ),
            new Stat(Title: "AWS Accounts", Value: await _dbContext.AwsAccounts.CountAsync()),
            new Stat(Title: "Kubernetes Clusters", Value: 1),
            new Stat(Title: "Kafka Clusters", Value: await _dbContext.KafkaClusters.Where(x => x.Enabled).CountAsync()),
            new Stat(
                Title: "Public Topics",
                Value: await _dbContext.KafkaTopics.Where(x => ((string)x.Name).StartsWith("pub.")).CountAsync()
            ),
            new Stat(
                Title: "Private Topics",
                Value: await _dbContext.KafkaTopics.Where(x => !((string)x.Name).StartsWith("pub.")).CountAsync()
            ),
        };
    }
}

public class UpdatePersonalInformationRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class MyProfileApiResource
{
    public string Id { get; set; } = "";
    public IEnumerable<CapabilityListItemApiResource> Capabilities { get; set; } =
        Enumerable.Empty<CapabilityListItemApiResource>();
    public bool AutoReloadTopics { get; set; } = true;
    public PersonalInformationApiResource PersonalInformation { get; set; } = new();

    [JsonPropertyName("_links")]
    public MyProfileLinks Links { get; set; } = new();

    public class MyProfileLinks
    {
        public ResourceLink Self { get; set; } = new();
        public ResourceLink PersonalInformation { get; set; } = new();
        public ResourceLink PortalVisits { get; set; } = new();
        public ResourceLink TopVisitors { get; set; } = new();
    }
}

public class PersonalInformationApiResource
{
    public static PersonalInformationApiResource Empty = new();

    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public record Stat(string Title, int Value); // TODO [jandr@2023-04-20]: make this a domain concept maybe?!
