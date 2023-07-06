using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;


namespace SelfService.Infrastructure.BackgroundJobs;

public class RemoveInactiveMemberships : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RemoveInactiveMemberships> _logger;

    public RemoveInactiveMemberships(IServiceProvider serviceProvider, ILogger<RemoveInactiveMemberships> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWork(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
            }
        }, stoppingToken);
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        using var _ = _logger.BeginScope("{BackgroundJob} {CorrelationId}",
            nameof(RemoveInactiveMemberships), Guid.NewGuid());

        var membershipCleaner = scope.ServiceProvider.GetRequiredService<InactiveMembershipCleaner>();

        _logger.LogDebug("Removing inactive/deleted users' memberships...");
        await membershipCleaner.CleanupMemberships();
    }
}
