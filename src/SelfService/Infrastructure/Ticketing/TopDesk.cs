using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Ticketing;

public class TopDesk : ITicketingSystem
{
    private readonly HttpClient _httpClient;

    public TopDesk(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CreateTicket(string message)
    {
        using var content = new StringContent(message);

        var response = await _httpClient.PostAsync("sendnotification", content);

        response.EnsureSuccessStatusCode();
    }
}