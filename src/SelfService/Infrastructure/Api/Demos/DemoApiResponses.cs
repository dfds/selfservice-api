using System.Text.Json.Serialization;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Demos;

public class DemoSignupApiResource
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
}

public class DemoSignupsApiResponse
{
    public List<DemoSignupApiResource> Items { get; set; } = new();

    [JsonPropertyName("_links")]
    public required DemoSignupsApiResponseLinks Links { get; set; } = null!;

    public class DemoSignupsApiResponseLinks
    {
        public ResourceLink? Self { get; set; }
    }
}
