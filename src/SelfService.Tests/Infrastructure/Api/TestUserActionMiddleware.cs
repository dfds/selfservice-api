using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using SelfService.Domain.Events;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api;
using SelfService.Infrastructure.Messaging;

namespace SelfService.Tests.Infrastructure.Api;

public class TestUserActionMiddleware
{
    [Fact]
    public async Task skips_request_data_when_skip_attribute_is_present()
    {
        var messaging = new CapturingMessagingService();
        var context = CreateContext("binary-image-bytes", withSkipRequestData: true);

        await new UserActionMiddleware(messaging).InvokeAsync(context, _ => Task.CompletedTask);

        var evt = Assert.IsType<UserAction>(Assert.Single(messaging.Events));
        Assert.Equal("", evt.RequestData);
    }

    [Fact]
    public async Task captures_request_data_when_skip_attribute_is_absent()
    {
        var messaging = new CapturingMessagingService();
        var context = CreateContext("{\"name\":\"value\"}", withSkipRequestData: false);

        await new UserActionMiddleware(messaging).InvokeAsync(context, _ => Task.CompletedTask);

        var evt = Assert.IsType<UserAction>(Assert.Single(messaging.Events));
        Assert.Equal("{\"name\":\"value\"}", evt.RequestData);
    }

    private static HttpContext CreateContext(string body, bool withSkipRequestData)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "tester@dfds.com") }, "test")
            ),
        };
        context.Request.Method = "POST";
        context.Request.Path = "/private-assets/upload";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var metadata = withSkipRequestData
            ? new EndpointMetadataCollection(new UserActionSkipRequestDataAttribute())
            : EndpointMetadataCollection.Empty;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "test-endpoint"));

        return context;
    }

    private sealed class CapturingMessagingService : IMessagingService
    {
        public List<IDomainEvent> Events { get; } = new();

        public Task SendDomainEvent(IDomainEvent evt)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }
    }
}
