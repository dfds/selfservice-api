using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using SelfService.Infrastructure.Catalog;

namespace SelfService.Tests.Infrastructure.Catalog;

public class TestCatalogClient
{
    private static HttpClient MockHttp(HttpStatusCode status, string body)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = status, Content = new StringContent(body) });
        return new HttpClient(handler.Object);
    }

    private static ICatalogTokenProvider NoToken()
    {
        var provider = new Mock<ICatalogTokenProvider>();
        provider.Setup(x => x.GetAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        return provider.Object;
    }

    [Fact]
    public async Task GetCatalog_unwraps_envelope_and_deserializes_camelCase()
    {
        const string json = """
            {
              "data": {
                "cluster": "local",
                "applications": [
                  {
                    "namespace": "team-alpha-abcde",
                    "name": "api",
                    "kind": "Deployment",
                    "capabilityId": "team-alpha-abcde",
                    "replicas": 3,
                    "readyReplicas": 2,
                    "services": [
                      { "name": "svc", "type": "ClusterIP", "apiDocs": [ { "path": "/swagger", "url": "http://svc/swagger" } ] }
                    ]
                  }
                ],
                "namespaces": [],
                "dependencies": [],
                "collectedAt": "2026-01-01T00:00:00Z"
              },
              "meta": { "collectedAt": "2026-01-01T00:00:00Z", "cluster": "local" }
            }
            """;

        var client = new CatalogClient(
            MockHttp(HttpStatusCode.OK, json),
            NoToken(),
            NullLogger<CatalogClient>.Instance
        );

        var snapshot = await client.GetCatalog(new Uri("http://ssu-catalog:8080"));

        Assert.NotNull(snapshot);
        Assert.Equal("local", snapshot!.Cluster);
        var app = Assert.Single(snapshot.Applications);
        Assert.Equal("api", app.Name);
        Assert.Equal(3, app.Replicas);
        Assert.Equal(2, app.ReadyReplicas);
        var service = Assert.Single(app.Services);
        Assert.Equal("/swagger", Assert.Single(service.ApiDocs).Path);
    }

    [Fact]
    public async Task GetCatalog_deserializes_author_declared_metadata()
    {
        const string json = """
            {
              "data": {
                "cluster": "local",
                "applications": [
                  {
                    "namespace": "team-alpha-abcde",
                    "name": "api",
                    "kind": "Deployment",
                    "capabilityId": "team-alpha-abcde",
                    "metadata": {
                      "description": "Handles billing",
                      "links": [
                        { "label": "dashboard", "url": "https://grafana/d/api" },
                        { "label": "runbook", "url": "https://wiki/runbooks/api" }
                      ]
                    }
                  }
                ],
                "namespaces": [],
                "dependencies": [],
                "collectedAt": "2026-01-01T00:00:00Z"
              },
              "meta": { "collectedAt": "2026-01-01T00:00:00Z", "cluster": "local" }
            }
            """;

        var client = new CatalogClient(
            MockHttp(HttpStatusCode.OK, json),
            NoToken(),
            NullLogger<CatalogClient>.Instance
        );

        var snapshot = await client.GetCatalog(new Uri("http://ssu-catalog:8080"));

        var app = Assert.Single(snapshot!.Applications);
        Assert.NotNull(app.Metadata);
        Assert.Equal("Handles billing", app.Metadata!.Description);
        Assert.Equal(2, app.Metadata.Links.Count);
        Assert.Equal("dashboard", app.Metadata.Links[0].Label);
        Assert.Equal("https://grafana/d/api", app.Metadata.Links[0].Url);
    }

    [Fact]
    public async Task GetCatalog_deserializes_all_repo_urls_and_null_coalesces()
    {
        // ssu-catalog emits repoUrls[] carrying the discovered GitOps repo *and*
        // any author-declared dfds.cloud/repo, deduped & discovered-first. The
        // whole array must survive so the declared repo isn't dropped when a
        // GitOps source is found. A workload without repos marshals as null.
        const string json = """
            {
              "data": {
                "cluster": "local",
                "applications": [
                  {
                    "namespace": "ns-a", "name": "gitops-plus-declared", "kind": "Deployment", "capabilityId": "ns-a",
                    "repoUrls": [ "https://github.com/dfds/ssu-apps", "https://github.com/dfds/api" ],
                    "deploymentSource": { "tool": "argocd", "repoUrl": "https://github.com/dfds/ssu-apps" }
                  },
                  { "namespace": "ns-b", "name": "no-repos", "kind": "Deployment", "capabilityId": "ns-b", "repoUrls": null }
                ],
                "namespaces": [], "dependencies": [], "collectedAt": "2026-01-01T00:00:00Z"
              },
              "meta": { "collectedAt": "2026-01-01T00:00:00Z", "cluster": "local" }
            }
            """;

        var client = new CatalogClient(
            MockHttp(HttpStatusCode.OK, json),
            NoToken(),
            NullLogger<CatalogClient>.Instance
        );

        var snapshot = await client.GetCatalog(new Uri("http://ssu-catalog:8080"));

        var both = snapshot!.Applications.Single(a => a.Name == "gitops-plus-declared");
        Assert.Equal(
            new[] { "https://github.com/dfds/ssu-apps", "https://github.com/dfds/api" },
            both.RepoUrls
        );
        Assert.Equal("https://github.com/dfds/ssu-apps", both.DeploymentSource!.RepoUrl);

        var none = snapshot.Applications.Single(a => a.Name == "no-repos");
        Assert.NotNull(none.RepoUrls);
        Assert.Empty(none.RepoUrls);
    }

    [Fact]
    public async Task GetCatalog_metadata_absent_stays_null_and_null_links_coalesce()
    {
        // metadata absent on the first app; present-with-null-links on the second
        // (Go marshals a nil slice as JSON null). The coalescing setter must keep
        // Links non-null so downstream mapping/enumeration is safe.
        const string json = """
            {
              "data": {
                "cluster": "local",
                "applications": [
                  { "namespace": "ns-a", "name": "no-meta", "kind": "Deployment", "capabilityId": "ns-a" },
                  {
                    "namespace": "ns-b", "name": "desc-only", "kind": "Deployment", "capabilityId": "ns-b",
                    "metadata": { "description": "Just a description", "links": null }
                  }
                ],
                "namespaces": [], "dependencies": [], "collectedAt": "2026-01-01T00:00:00Z"
              },
              "meta": { "collectedAt": "2026-01-01T00:00:00Z", "cluster": "local" }
            }
            """;

        var client = new CatalogClient(
            MockHttp(HttpStatusCode.OK, json),
            NoToken(),
            NullLogger<CatalogClient>.Instance
        );

        var snapshot = await client.GetCatalog(new Uri("http://ssu-catalog:8080"));

        var noMeta = snapshot!.Applications.Single(a => a.Name == "no-meta");
        Assert.Null(noMeta.Metadata);

        var descOnly = snapshot.Applications.Single(a => a.Name == "desc-only");
        Assert.NotNull(descOnly.Metadata);
        Assert.Equal("Just a description", descOnly.Metadata!.Description);
        Assert.NotNull(descOnly.Metadata.Links);
        Assert.Empty(descOnly.Metadata.Links);
    }

    [Fact]
    public async Task GetCatalog_deserializes_reachability_and_null_coalesces()
    {
        // First service carries a reachability overlay; the second omits it entirely
        // (Go's omitempty drops the key). The coalescing setter must leave Reachability
        // non-null and empty so downstream mapping/enumeration is safe.
        const string json = """
            {
              "data": {
                "cluster": "local",
                "applications": [
                  {
                    "namespace": "team-alpha-abcde",
                    "name": "api",
                    "kind": "Deployment",
                    "capabilityId": "team-alpha-abcde",
                    "services": [
                      {
                        "name": "svc-a",
                        "externalHosts": [ "api.example.com" ],
                        "reachability": [
                          {
                            "host": "api.example.com",
                            "url": "https://api.example.com/",
                            "status": "reachable",
                            "statusCode": 200,
                            "expected": "200",
                            "reason": "",
                            "checkedAt": "2026-07-10T12:00:00Z"
                          }
                        ]
                      },
                      { "name": "svc-b", "reachability": null }
                    ]
                  }
                ],
                "namespaces": [],
                "dependencies": [],
                "collectedAt": "2026-01-01T00:00:00Z"
              },
              "meta": { "collectedAt": "2026-01-01T00:00:00Z", "cluster": "local" }
            }
            """;

        var client = new CatalogClient(
            MockHttp(HttpStatusCode.OK, json),
            NoToken(),
            NullLogger<CatalogClient>.Instance
        );

        var snapshot = await client.GetCatalog(new Uri("http://ssu-catalog:8080"));

        Assert.NotNull(snapshot);
        var app = Assert.Single(snapshot!.Applications);
        Assert.Equal(2, app.Services.Count);

        var svcA = app.Services[0];
        var reach = Assert.Single(svcA.Reachability);
        Assert.Equal("api.example.com", reach.Host);
        Assert.Equal("reachable", reach.Status);
        Assert.Equal(200, reach.StatusCode);
        Assert.Equal("200", reach.Expected);

        // Absent/null reachability coalesces to a non-null empty list.
        var svcB = app.Services[1];
        Assert.NotNull(svcB.Reachability);
        Assert.Empty(svcB.Reachability);
    }

    [Fact]
    public async Task GetCatalog_returns_null_on_non_success_status()
    {
        var client = new CatalogClient(
            MockHttp(HttpStatusCode.InternalServerError, "boom"),
            NoToken(),
            NullLogger<CatalogClient>.Instance
        );

        var snapshot = await client.GetCatalog(new Uri("http://ssu-catalog:8080"));

        Assert.Null(snapshot);
    }
}
