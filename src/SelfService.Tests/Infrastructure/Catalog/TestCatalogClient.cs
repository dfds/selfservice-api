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
