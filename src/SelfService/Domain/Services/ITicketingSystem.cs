namespace SelfService.Domain.Services;

public interface ITicketingSystem
{
    public Task CreateTicket(string message, IDictionary<string, string> headers);
}
