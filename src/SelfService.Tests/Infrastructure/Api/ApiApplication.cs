using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Application;
using SelfService.Infrastructure.Messaging;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class ApiApplication : WebApplicationFactory<Program>
{
    private readonly List<Action<IServiceCollection>> _serviceCollectionModifiers = new();
    private readonly Dictionary<string, string?> _customConfiguration = new();
    private readonly string _environment;
    private readonly bool _overrideDb;

    private Action<FakeAuthenticationSchemeOptions>? _authOptionsConfig;

    public ApiApplication(bool configureForProduction = true, bool overrideDb = false)
    {
        _environment = configureForProduction ? Environments.Production : Environments.Development;
        _overrideDb = overrideDb;
        this.ReplaceService<IMessagingService>(new StubMessagingService());
    }

    public ApiApplication ConfigureService(Action<IServiceCollection> cfg)
    {
        _serviceCollectionModifiers.Add(cfg);
        return this;
    }

    public ApiApplication RemoveService<TServiceType>()
    {
        ConfigureService(services =>
        {
            var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(TServiceType));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
        });

        return this;
    }

    public ApiApplication ReplaceService<TServiceType>(TServiceType implementation)
    {
        ConfigureService(services =>
        {
            var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(TServiceType));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var newDescriptor = ServiceDescriptor.Describe(
                serviceType: typeof(TServiceType),
                implementationFactory: _ => implementation!,
                lifetime: descriptor?.Lifetime ?? ServiceLifetime.Transient
            );

            services.Add(newDescriptor);
        });

        return this;
    }

    public ApiApplication ReplaceConfiguration(string key, string value)
    {
        _customConfiguration[key] = value;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureLogging(cfg =>
        {
            cfg.ClearProviders();
            cfg.Services.RemoveAll<ILogger>();
            cfg.Services.RemoveAll<ILoggerFactory>();
            cfg.Services.AddTransient<ILoggerFactory, NullLoggerFactory>();
        });

        builder.UseSetting("DEFAULT_KAFKA_BOOTSTRAP_SERVERS", "dummy value");
        builder.UseSetting("DEFAULT_KAFKA_GROUP_ID", "dummy value");
        builder.UseSetting("SS_APISPECS_TOPIC", "dummy");
        builder.UseSetting("SS_TOPDESK_API_GATEWAY_ENDPOINT", "http://localhost:5051");
        builder.UseSetting("SS_TOPDESK_API_GATEWAY_API_KEY", "dummy");
        builder.UseSetting("SS_PROMETHEUS_API_ENDPOINT", "http://localhost:9090");
        builder.UseSetting("SS_PLATFORM_DATA_ENDPOINT", "http://localhost:8070");
        builder.UseSetting("SS_PLATFORM_DATA_API_KEY", "dummykey");
        builder.UseSetting("SS_CONFLUENT_GATEWAY_API_ENDPOINT", "http://localhost:8080");
        builder.UseSetting("SS_MIDDLEWARE_AUTHCHECKER_DISABLE", "true");

        builder.ConfigureAppConfiguration(x =>
        {
            // [thfis] need to load appsettings.json otherwise the AzureAd security section isn't loaded
            // x.Sources.Clear();

            if (_customConfiguration.Any())
            {
                x.AddInMemoryCollection(_customConfiguration);
            }
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHealthCheck>();
            services.RemoveAll<IHostedService>();
        });

        builder.ConfigureTestServices(services =>
        {
            // todo: WHHYYYYYYYY https://github.com/dotnet/efcore/blob/34d1953667427d870e768a99116112522953fd0c/src/EFCore/Extensions/EntityFrameworkServiceCollectionExtensions.cs#L881-L898
            if (_overrideDb)
            {
                foreach (var option in services.Where(s => s.ServiceType.BaseType == typeof(DbContextOptions)).ToList())
                {
                    services.Remove(option);
                }                
            }

            
            _serviceCollectionModifiers.ForEach(cfg => cfg(services));

            if (_authOptionsConfig != null)
            {
                services.Configure(_authOptionsConfig);
            }

            services
                .AddAuthentication(FakeAuthenticationSchemeDefaults.AuthenticationScheme)
                .AddScheme<FakeAuthenticationSchemeOptions, FakeAuthenticationHandler>(
                    FakeAuthenticationSchemeDefaults.AuthenticationScheme,
                    null
                );
        });
    }

    public void ConfigureFakeAuthentication(Action<FakeAuthenticationSchemeOptions> authOptionsConfig)
    {
        _authOptionsConfig = authOptionsConfig;
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        // set test authentication scheme by default
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            FakeAuthenticationSchemeDefaults.AuthenticationScheme
        );
    }
}
