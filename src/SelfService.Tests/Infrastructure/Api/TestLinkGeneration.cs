using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestLinkGeneration
{
    public static readonly string KafkaTopicId = SelfService.Domain.Models.KafkaTopicId.Parse(Guid.Empty.ToString());
    public static readonly string MessageContractId = SelfService.Domain.Models.MessageContractId.Parse(Guid.Empty.ToString());

    [Fact]
    public async Task can_generate_expected_urls()
    {
        await using var application = new ApiApplication();
        application.ConfigureService(services => { services.AddControllers().AddApplicationPart(typeof(TestLinkGenerationController).Assembly).AddControllersAsServices(); });
        using var client = application.CreateClient();

        var response = await client.GetAsync("/routes");
        response.EnsureSuccessStatusCode();
        var links = await response.Content.ReadFromJsonAsync<string[]>();

        Assert.Equal(new[]
        {
            "http://localhost/capabilities",
            $"http://localhost/kafkatopics/{KafkaTopicId}",
            $"http://localhost/kafkatopics/{KafkaTopicId}/messagecontracts/{MessageContractId}"
        }, links);
    }
}

[Route("/routes")]
[ApiController]
public class TestLinkGenerationController : ControllerBase
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public TestLinkGenerationController(IHttpContextAccessor contextAccessor, LinkGenerator linkGenerator)
    {
        _contextAccessor = contextAccessor;
        _linkGenerator = linkGenerator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRoutes()
    {
        // mock security as that doesn't affect link URLs
        var mock = new Mock<IAuthorizationService>();
        mock.SetReturnsDefault(Task.FromResult(AuthorizationResult.Success()));
        var factory = new ApiResourceFactory(_contextAccessor, _linkGenerator, mock.Object, Dummy.Of<SelfService.Domain.Services.IAuthorizationService>());

        var topic = A.KafkaTopic
            .WithId(TestLinkGeneration.KafkaTopicId)
            .Build();

        var links = new[]
        {
            factory.Convert(Array.Empty<Capability>()).Links.Self.Href,
            (await factory.Convert(topic)).Links.Self.Href,
            factory.Convert(A.MessageContract.WithId(TestLinkGeneration.MessageContractId).WithKafkaTopicId(TestLinkGeneration.KafkaTopicId)).Links.Self.Href
        };

        return Ok(links);
    }
    
    private static class A
    {
        public static MessageContractBuilder MessageContract => new();
        public static KafkaTopicBuilder KafkaTopic => new();
    }
}