using Aspectly;
using Dafda.Outbox;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Messaging;

//public class OutboxEnqueuerAspect : IAspect
//{
//    private readonly ILogger<OutboxEnqueuerAspect> _logger;
//    private readonly SelfServiceDbContext _dbContext;
//    private readonly OutboxQueue _outbox;

//    public OutboxEnqueuerAspect(ILogger<OutboxEnqueuerAspect> logger, SelfServiceDbContext dbContext, OutboxQueue outbox)
//    {
//        _logger = logger;
//        _dbContext = dbContext;
//        _outbox = outbox;
//    }

//    public async Task Invoke(AspectContext context, AspectDelegate next)
//    {
//        await next();
//        await EnqueueDomainEvents();
//    }

//    private async Task EnqueueDomainEvents()
//    {
//        var aggregateDomainEvents = GetAggregates();

//        if (aggregateDomainEvents.Length > 1)
//        {
//            // TODO [jandr@2023-02-23]: rethink this - maybe remove
//            throw new InvalidOperationException("Multiple aggregates modified in the same transaction");
//        }

//        foreach (var aggregate in aggregateDomainEvents)
//        {
//            var domainEvents = aggregate
//                .GetEvents()
//                .ToArray();

//            await _outbox.Enqueue(domainEvents);
//            aggregate.ClearEvents();

//            foreach (var domainEvent in domainEvents)
//            {
//                _logger.LogTrace("Enqueued domain event {DomainEvent} in the outbox for aggregate {Aggregate}", 
//                    domainEvent.GetType().Name, aggregate.GetType().Name);
//            }
//        }
//    }

//    private IEventSource[] GetAggregates()
//    {
//        return _dbContext
//            .ChangeTracker
//            .Entries<IEventSource>()
//            .Where(x => x.Entity.GetEvents().Any())
//            .Select(x => x.Entity)
//            .ToArray();
//    }
//}