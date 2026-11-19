using Microsoft.Extensions.DependencyInjection;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests;

internal static class TransactionalServiceFactory
{
    public static ServiceProvider Create(SelfServiceDbContext db, INewsItemRepository? newsRepository = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddTransient<IDbTransactionFacade, RealDbTransactionFacade>();
        services.AddTransient<IPrivateAssetRepository, PrivateAssetRepository>();
        services.AddTransient<IEventRepository, EventRepository>();
        services.AddTransient<IEventAttachmentRepository, EventAttachmentRepository>();
        if (newsRepository is null)
            services.AddTransient<INewsItemRepository, NewsItemRepository>();
        else
            services.AddSingleton(newsRepository);
        services.AddTransient<INewsItemService, NewsItemService>();
        services.AddTransient<IEventService, EventService>();
        services.RewireWithAspects(options => options.Register<TransactionalBoundaryAttribute, TransactionalAspect>());
        return services.BuildServiceProvider();
    }
}
