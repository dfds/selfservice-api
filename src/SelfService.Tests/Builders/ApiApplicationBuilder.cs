using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SelfService.Application;
using SelfService.Configuration;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Messaging;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Queries;
using SelfService.Tests.Infrastructure.Api;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;


public class ApiApplicationBuilder
{
    private IAwsAccountRepository _awsAccountRepository;
    private ICapabilityRepository _capabilityRepository;
    private IReleaseNoteRepository _releaseNoteRepository;
    private IMembershipQuery _membershipQuery;
    private ICapabilityDeletionStatusQuery _capabilityDeletionStatusQuery;
    private Action<IServiceCollection> _configureRbac;
    private Action<IServiceCollection> _configureDb;
    private const string DefaultConnectionString =
        "User ID=postgres;Password=p;Host=localhost;Port=5432;Database=db;timeout=2;Command Timeout=2;";

    private bool _overrideDb = false;

    public ApiApplicationBuilder()
    {
        _awsAccountRepository = new StubAwsAccountRepository();
        _capabilityRepository = new StubCapabilityRepository();
        _releaseNoteRepository = new StubReleaseNoteRepository();
        _membershipQuery = new StubMembershipQuery();
        _capabilityDeletionStatusQuery = new StubCapabilityDeletionStatusQuery();
        _configureRbac = cfg => { };
        _configureDb = cfg => { };
    }

    public ApiApplicationBuilder WithAwsAccountRepository(IAwsAccountRepository awsAccountRepository)
    {
        _awsAccountRepository = awsAccountRepository;
        return this;
    }

    public ApiApplicationBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public ApiApplicationBuilder WithMembershipQuery(IMembershipQuery membershipQuery)
    {
        _membershipQuery = membershipQuery;
        return this;
    }

    public ApiApplicationBuilder WithCapabilityDeletionStatusQuery(
        ICapabilityDeletionStatusQuery capabilityDeletionStatusQuery
    )
    {
        _capabilityDeletionStatusQuery = capabilityDeletionStatusQuery;
        return this;
    }

    public ApiApplicationBuilder WithReleaseNoteRepository(IReleaseNoteRepository releaseNoteRepository)
    {
        _releaseNoteRepository = releaseNoteRepository;
        return this;
    }

    public ApiApplicationBuilder ConfigureRbac()
    {
        _configureRbac = svc =>
        {
            svc.AddTransient<IRbacPermissionGrantRepository, RbacPermissionGrantRepository>();
            svc.AddTransient<IRbacRoleGrantRepository, RbacRoleGrantRepository>();
            svc.AddTransient<IRbacApplicationService, RbacApplicationService>();
        };
        return this;
    }

    public Task<ApiApplicationBuilder> WithSelfServiceDbContext(SelfServiceDbContext selfServiceDbContext)
    {
        _overrideDb = true;
        _configureDb = svc =>
        {
            svc.AddSingleton<SelfServiceDbContext>(selfServiceDbContext);
        };

        return Task.FromResult(this);
    }

    public Task<ApiApplicationBuilder> WithLocalDb()
    {
        _overrideDb = true;
        _configureDb = svc =>
        {
            svc.AddDbContext<SelfServiceDbContext>(opts =>
            {
                opts.UseNpgsql(Environment.GetEnvironmentVariable("SS_CONNECTION_STRING") ?? DefaultConnectionString);
            });
        };

        return Task.FromResult(this);
    }

    public ApiApplication Build()
    {
        var application = new ApiApplication(overrideDb: _overrideDb);
        application.ReplaceService<IAwsAccountRepository>(_awsAccountRepository);
        application.ReplaceService<ICapabilityRepository>(_capabilityRepository);
        application.ReplaceService<IReleaseNoteRepository>(_releaseNoteRepository);
        application.ReplaceService<IMembershipQuery>(_membershipQuery);
        application.ReplaceService<ICapabilityDeletionStatusQuery>(_capabilityDeletionStatusQuery);
        application.ReplaceService<IMessagingService>(new StubMessagingService());

        application.ConfigureService(cfg =>
        {
            _configureDb(cfg);
            _configureRbac(cfg);
        });
        return application;
    }
}

public static class ApiApplicationBuilderExtensions
{
    public static async Task<ApiApplicationBuilder> ConfigureRbac(this Task<ApiApplicationBuilder> builderTask)
    {
        var builder = await builderTask;
        return builder.ConfigureRbac();
    }

    public static async Task<ApiApplicationBuilder> WithSelfServiceDbContext(
        this Task<ApiApplicationBuilder> builderTask, SelfServiceDbContext selfServiceDbContext)
    {
        var builder = await builderTask;
        return await builder.WithSelfServiceDbContext(selfServiceDbContext);
    }

    public static async Task<ApiApplicationBuilder> WithLocalDb(
        this Task<ApiApplicationBuilder> builderTask)
    {
        var builder = await builderTask;
        return await builder.WithLocalDb();
    }

    public static async Task<ApiApplication> BuildAsync(this Task<ApiApplicationBuilder> builderTask)
    {
        var builder = await builderTask;
        return builder.Build();
    }
}