using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Ticketing;

public class TopDesk : ITicketingSystem
{
    private readonly HttpClient _httpClient;

    public TopDesk(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CreateTicket(string message, IDictionary<string, string> headers)
    {
        using var content = new StringContent(message);
        using var request = new HttpRequestMessage(HttpMethod.Post, "sendnotification")
        {
            Content = content
        };

        foreach (var (key, value) in headers)
        {
            request.Headers.Add(key, value);
        }

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }
}