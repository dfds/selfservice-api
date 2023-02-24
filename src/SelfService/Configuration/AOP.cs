using SelfService.Domain;
using SelfService.Infrastructure.Messaging;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Configuration;

public static class AOP
{
    public static void ConfigureAspects(this WebApplicationBuilder builder)
    {
        builder.Services.RewireWithAspects(options =>
        {
            options.Register<TransactionalBoundaryAttribute, TransactionalAspect>();
            //options.Register<OutboxedAttribute, OutboxEnqueuerAspect>();
        });
    }
}