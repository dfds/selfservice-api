using System.Net.Http.Headers;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Ticketing;

public class TopDesk : ITicketingSystem
{
    private readonly ILogger<TopDesk> _logger;
    private readonly HttpClient _httpClient;

    public TopDesk(ILogger<TopDesk> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task CreateTicket(string message, IDictionary<string, string> headers)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", nameof(CreateTicket), GetType().FullName);

        using var content = new StringContent(message, new MediaTypeHeaderValue("text/plain"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "sendnotification") { Content = content };

        foreach (var (key, value) in headers)
        {
            request.Headers.Add(key, value);
        }

        _logger.LogDebug(
            "Going to create top desk ticket {TopDeskTicket} at {TopDeskTicketUrl}",
            message,
            _httpClient.BaseAddress
        );

        var response = await _httpClient.SendAsync(request);

        _logger.LogDebug(
            "Response was {StatusCode} with body {ResponseBody}",
            response.StatusCode,
            await response.Content.ReadAsStringAsync()
        );

        response.EnsureSuccessStatusCode();
    }
}
