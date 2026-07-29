using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Catalog;
using SelfService.Tests.Builders;

namespace SelfService.Tests.Application;

public class TestCatalogApplicationService
{
    private static CatalogApplicationService BuildService(
        CatalogConfig config,
        ICatalogClient catalogClient,
        ICapabilityRepository capabilityRepository,
        TimeSpan? ttl = null
    ) => new(BuildCache(config, catalogClient, capabilityRepository, ttl));

    /// The cache resolves the fetcher from a scope of its own, so tests wire a small
    /// real container around the mocks rather than handing it a fetcher directly.
    private static CatalogSnapshotCache BuildCache(
        CatalogConfig config,
        ICatalogClient catalogClient,
        ICapabilityRepository capabilityRepository,
        TimeSpan? ttl = null
    )
    {
        var services = new ServiceCollection();
        services.AddSingleton(config);
        services.AddSingleton(catalogClient);
        services.AddSingleton(capabilityRepository);
        services.AddSingleton<ILogger<CatalogFetcher>>(NullLogger<CatalogFetcher>.Instance);
        services.AddTransient<ICatalogFetcher, CatalogFetcher>();
        var provider = services.BuildServiceProvider();

        return new CatalogSnapshotCache(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CatalogSnapshotCache>.Instance,
            ttl ?? CatalogSnapshotCache.DefaultTtl
        );
    }

    private static CatalogConfig SingleCluster(string cluster = "local") =>
        new(new[] { new CatalogClusterEndpoint(cluster, new Uri("http://ssu-catalog:8080")) }, scope: "");

    [Fact]
    public void ParseEndpoints_parses_cluster_url_csv()
    {
        var result = CatalogConfig.ParseEndpoints("prod=https://a.example, dev=https://b.example");

        Assert.Equal(2, result.Count);
        Assert.Equal("prod", result[0].Cluster);
        Assert.Equal(new Uri("https://a.example"), result[0].Url);
        Assert.Equal("dev", result[1].Cluster);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("no-equals-sign")]
    [InlineData("=https://missing-cluster")]
    [InlineData("cluster=")]
    [InlineData("cluster=not a uri")]
    public void ParseEndpoints_skips_blank_and_malformed(string? raw)
    {
        Assert.Empty(CatalogConfig.ParseEndpoints(raw));
    }

    [Fact]
    public async Task Merge_keeps_only_capability_owned_apps_and_joins_name()
    {
        var capability = A.Capability.WithId(CapabilityId.Parse("team-alpha-abcde")).WithName("Team Alpha").Build();

        var snapshot = new CatalogSnapshotDto
        {
            Applications =
            {
                new ApplicationEntryDto
                {
                    Namespace = "team-alpha-abcde",
                    Name = "api",
                    Kind = "Deployment",
                    CapabilityId = "team-alpha-abcde",
                },
                new ApplicationEntryDto
                {
                    Namespace = "orphan-xyzab",
                    Name = "stray",
                    Kind = "Deployment",
                    CapabilityId = "orphan-xyzab", // no matching capability → filtered out
                },
            },
        };

        var catalogClient = new Mock<ICatalogClient>();
        catalogClient.Setup(x => x.GetCatalog(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new[] { capability });

        var service = BuildService(SingleCluster(), catalogClient.Object, capabilityRepository.Object);

        var result = await service.ListApplications(new ApplicationFilters());

        var app = Assert.Single(result.Items);
        Assert.Equal("api", app.Name);
        Assert.Equal("Team Alpha", app.CapabilityName);
        Assert.Equal("local", app.Cluster); // stamped from the registry
        Assert.True(result.Availability.CatalogAvailable);
        Assert.Equal(1, result.Availability.ClustersQueried);
        Assert.Equal(0, result.Availability.ClustersFailed);
    }

    [Fact]
    public async Task GetDependencies_keeps_edges_owned_on_either_end()
    {
        var capability = A.Capability.WithId(CapabilityId.Parse("team-alpha-abcde")).WithName("Team Alpha").Build();

        // Nodes carry ssu-catalog's payload cluster ("local" here == the registry key);
        // only apps/namespaces get the registry cluster re-stamped on merge.
        DependencyNodeDto Owned() =>
            new()
            {
                Cluster = "local",
                Namespace = "team-alpha-abcde",
                Service = "api",
                External = false,
            };
        DependencyNodeDto External(string service) => new() { Service = service, External = true };

        var snapshot = new CatalogSnapshotDto
        {
            Applications =
            {
                new ApplicationEntryDto
                {
                    Namespace = "team-alpha-abcde",
                    Name = "api",
                    Kind = "Deployment",
                    CapabilityId = "team-alpha-abcde",
                },
            },
            Dependencies =
            {
                // Outbound: owned app → external. Kept (source owned).
                new DependencyEdgeDto { Source = Owned(), Target = External("rds.example.com") },
                // Inbound: another workload → owned app. Kept (target owned) — this is the fix;
                // a source-only filter would drop it and hide "who connects to me".
                new DependencyEdgeDto { Source = External("ssu-catalog"), Target = Owned() },
                // Neither end owned. Dropped.
                new DependencyEdgeDto { Source = External("x"), Target = External("y") },
            },
        };

        var catalogClient = new Mock<ICatalogClient>();
        catalogClient.Setup(x => x.GetCatalog(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new[] { capability });

        var service = BuildService(SingleCluster(), catalogClient.Object, capabilityRepository.Object);

        var result = await service.GetDependencies(new DependencyFilters());

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, d => d.Source.Service == "api" && d.Target.Service == "rds.example.com");
        Assert.Contains(result.Items, d => d.Source.Service == "ssu-catalog" && d.Target.Service == "api");
        Assert.DoesNotContain(result.Items, d => d.Source.Service == "x");
    }

    [Fact]
    public async Task GetDeploymentsForCapability_filters_by_capability_id()
    {
        var alpha = A.Capability.WithId(CapabilityId.Parse("team-alpha-abcde")).WithName("Team Alpha").Build();
        var beta = A.Capability.WithId(CapabilityId.Parse("team-beta-fghij")).WithName("Team Beta").Build();

        var snapshot = new CatalogSnapshotDto
        {
            Applications =
            {
                new ApplicationEntryDto
                {
                    Namespace = "team-alpha-abcde",
                    Name = "api",
                    CapabilityId = "team-alpha-abcde",
                },
                new ApplicationEntryDto
                {
                    Namespace = "team-beta-fghij",
                    Name = "worker",
                    CapabilityId = "team-beta-fghij",
                },
            },
        };

        var catalogClient = new Mock<ICatalogClient>();
        catalogClient.Setup(x => x.GetCatalog(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new[] { alpha, beta });

        var service = BuildService(SingleCluster(), catalogClient.Object, capabilityRepository.Object);

        var result = await service.GetDeploymentsForCapability(CapabilityId.Parse("team-alpha-abcde"));

        var app = Assert.Single(result.Items);
        Assert.Equal("api", app.Name);
    }

    [Fact]
    public async Task HasDocs_filter_selects_apps_with_api_docs()
    {
        var capability = A.Capability.WithId(CapabilityId.Parse("team-alpha-abcde")).WithName("Team Alpha").Build();

        var snapshot = new CatalogSnapshotDto
        {
            Applications =
            {
                new ApplicationEntryDto
                {
                    Namespace = "team-alpha-abcde",
                    Name = "documented",
                    CapabilityId = "team-alpha-abcde",
                    Services =
                    {
                        new ServiceRefDto { Name = "svc", ApiDocs = { new ApiDocInfoDto { Path = "/swagger" } } },
                    },
                },
                new ApplicationEntryDto
                {
                    Namespace = "team-alpha-abcde",
                    Name = "undocumented",
                    CapabilityId = "team-alpha-abcde",
                },
            },
        };

        var catalogClient = new Mock<ICatalogClient>();
        catalogClient.Setup(x => x.GetCatalog(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new[] { capability });

        var service = BuildService(SingleCluster(), catalogClient.Object, capabilityRepository.Object);

        var withDocs = await service.ListApplications(new ApplicationFilters(HasDocs: true));
        Assert.Equal("documented", Assert.Single(withDocs.Items).Name);

        var withoutDocs = await service.ListApplications(new ApplicationFilters(HasDocs: false));
        Assert.Equal("undocumented", Assert.Single(withoutDocs.Items).Name);
    }

    [Fact]
    public async Task All_clusters_fail_reports_unavailable_with_no_items()
    {
        var catalogClient = new Mock<ICatalogClient>();
        catalogClient
            .Setup(x => x.GetCatalog(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogSnapshotDto?)null); // every cluster fails

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(Array.Empty<Capability>());

        var service = BuildService(SingleCluster(), catalogClient.Object, capabilityRepository.Object);

        var result = await service.ListApplications(new ApplicationFilters());

        Assert.Empty(result.Items);
        Assert.False(result.Availability.CatalogAvailable);
        Assert.Equal(1, result.Availability.ClustersQueried);
        Assert.Equal(1, result.Availability.ClustersFailed);
    }

    [Fact]
    public async Task Expired_snapshot_is_served_without_waiting_for_the_refresh()
    {
        var capability = A.Capability.WithId(CapabilityId.Parse("team-alpha-abcde")).WithName("Team Alpha").Build();
        var refreshReached = new TaskCompletionSource();
        var releaseRefresh = new TaskCompletionSource();
        var calls = 0;

        var catalogClient = new Mock<ICatalogClient>();
        catalogClient
            .Setup(x => x.GetCatalog(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(
                async (Uri _, CancellationToken _) =>
                {
                    // Every fetch after the first one hangs, standing in for a slow fan-out.
                    if (Interlocked.Increment(ref calls) > 1)
                    {
                        refreshReached.TrySetResult();
                        await releaseRefresh.Task;
                    }
                    return SingleAppSnapshot();
                }
            );

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new[] { capability });

        // Zero TTL: the snapshot is stale the moment it lands.
        var service = BuildService(
            SingleCluster(),
            catalogClient.Object,
            capabilityRepository.Object,
            ttl: TimeSpan.Zero
        );

        await service.ListApplications(new ApplicationFilters()); // cold start — this one does wait

        // The snapshot is now expired, so this call kicks off a refresh that will not
        // complete. It must still return, from the stale data, rather than block on it.
        var stale = await service.ListApplications(new ApplicationFilters()).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal("api", Assert.Single(stale.Items).Name);
        await refreshReached.Task.WaitAsync(TimeSpan.FromSeconds(10)); // the refresh did start
        releaseRefresh.SetResult();
    }

    [Fact]
    public async Task Cold_callers_share_a_single_fetch()
    {
        var capability = A.Capability.WithId(CapabilityId.Parse("team-alpha-abcde")).WithName("Team Alpha").Build();
        var firstCallReached = new TaskCompletionSource();
        var releaseFirstCall = new TaskCompletionSource();
        var calls = 0;

        var catalogClient = new Mock<ICatalogClient>();
        catalogClient
            .Setup(x => x.GetCatalog(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(
                async (Uri _, CancellationToken _) =>
                {
                    if (Interlocked.Increment(ref calls) == 1)
                    {
                        firstCallReached.TrySetResult();
                        await releaseFirstCall.Task;
                    }
                    return SingleAppSnapshot();
                }
            );

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new[] { capability });

        var service = BuildService(SingleCluster(), catalogClient.Object, capabilityRepository.Object);

        var first = service.ListApplications(new ApplicationFilters());
        await firstCallReached.Task.WaitAsync(TimeSpan.FromSeconds(10)); // a fetch is in flight
        var second = service.ListApplications(new ApplicationFilters()); // arrives mid-fetch
        releaseFirstCall.SetResult();

        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));

        // The second caller queued on the refresh gate and read the snapshot the first
        // one produced — an expiry must not fan out once per concurrent request.
        Assert.Equal(1, calls);
        Assert.Equal("api", Assert.Single((await second).Items).Name);
    }

    private static CatalogSnapshotDto SingleAppSnapshot() =>
        new()
        {
            Applications =
            {
                new ApplicationEntryDto
                {
                    Namespace = "team-alpha-abcde",
                    Name = "api",
                    Kind = "Deployment",
                    CapabilityId = "team-alpha-abcde",
                },
            },
        };

    [Fact]
    public async Task TokenProvider_unconfigured_scope_returns_null_without_acquiring()
    {
        var tokenAcquisition = new Mock<Microsoft.Identity.Web.ITokenAcquisition>();
        var config = new CatalogConfig(Array.Empty<CatalogClusterEndpoint>(), scope: "");

        var provider = new CatalogTokenProvider(
            config,
            tokenAcquisition.Object,
            NullLogger<CatalogTokenProvider>.Instance
        );

        var token = await provider.GetAccessToken();

        Assert.Null(token);
        tokenAcquisition.VerifyNoOtherCalls();
    }
}
