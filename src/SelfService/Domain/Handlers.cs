using Dafda.Consuming;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Domain;

public class Placeholder
{
    public const string EventType = "placeholder";

    public string Name { get; set; }
    public string Namespace { get; set; }
    public string OpenApiSpec { get; set; }

    public Placeholder(string name, string @namespace, string openApiSpec)
    {
        Name = name;
        Namespace = @namespace;
        OpenApiSpec = openApiSpec;
    }
}

public class PlaceholderHandler : IMessageHandler<Placeholder>
{
    private readonly SelfServiceDbContext _dbContext;

    public PlaceholderHandler(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Placeholder message, MessageHandlerContext context)
    {
        var serviceDescription = await _dbContext.ServiceCatalog.FirstOrDefaultAsync(x => x.Name == message.Name);
        if (serviceDescription != null)
        {
            serviceDescription.Spec = message.OpenApiSpec;
        }
        else
        {
            serviceDescription = new ServiceDescription
            (
                id: Guid.NewGuid(),
                name: message.Name,
                @namespace: message.Namespace,
                spec: message.OpenApiSpec,
                createdAt: DateTime.UtcNow
            );

            await _dbContext.AddAsync(serviceDescription);
        }

        await _dbContext.SaveChangesAsync();
    }
}
