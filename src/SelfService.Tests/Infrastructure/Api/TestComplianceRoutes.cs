using System.Net;
using System.Text.Json;
using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Tests.Infrastructure.Api;

public class TestComplianceRoutes
{
    private static readonly DateOnly Today = new(2026, 11, 6);
    private static readonly SystemTime FixedTime = new(() => new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc));

    private static ApiApplication CreateApplication(Mock<IComplianceApplicationService> service)
    {
        var application = new ApiApplication();
        application.ReplaceService<IComplianceApplicationService>(service.Object);
        application.ReplaceService(FixedTime);
        return application;
    }

    private static Mock<IComplianceApplicationService> ServiceReturningSummary()
    {
        var service = new Mock<IComplianceApplicationService>();
        service
            .Setup(x => x.GetComplianceSummary(It.IsAny<DateOnly?>()))
            .ReturnsAsync(new ComplianceSummaryResult { TotalCapabilities = 3 });
        return service;
    }

    [Theory]
    [InlineData("/compliance/summary")]
    [InlineData("/compliance/summary?date=2026-11-06")]
    public async Task absent_date_or_today_reads_live_data(string url)
    {
        var service = ServiceReturningSummary();
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.CacheControl);
        service.Verify(x => x.GetComplianceSummary(null), Times.Once);
    }

    [Fact]
    public async Task past_date_reads_snapshot_and_is_cacheable()
    {
        var service = ServiceReturningSummary();
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync("/compliance/summary?date=2026-11-05");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.Equal(TimeSpan.FromDays(1), response.Headers.CacheControl?.MaxAge);
        service.Verify(x => x.GetComplianceSummary(Today.AddDays(-1)), Times.Once);
    }

    [Theory]
    [InlineData("2026-11-07")]
    [InlineData("2026-13-40")]
    [InlineData("yesterday")]
    public async Task future_or_malformed_date_is_bad_request(string date)
    {
        var service = ServiceReturningSummary();
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync($"/compliance/summary?date={date}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        service.Verify(x => x.GetComplianceSummary(It.IsAny<DateOnly?>()), Times.Never);
    }

    [Fact]
    public async Task date_before_retention_is_not_found_without_reading()
    {
        var service = ServiceReturningSummary();
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var oldest = ComplianceRetention.OldestRetained(Today);
        var response = await client.GetAsync($"/compliance/summary?date={oldest.AddDays(-1):yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        service.Verify(x => x.GetComplianceSummary(It.IsAny<DateOnly?>()), Times.Never);
    }

    [Fact]
    public async Task day_without_snapshot_is_not_found()
    {
        var service = new Mock<IComplianceApplicationService>();
        service
            .Setup(x => x.GetRequirementsCompliance(It.IsAny<DateOnly?>()))
            .ThrowsAsync(new ComplianceSnapshotNotFoundException(Today.AddDays(-3)));
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync("/compliance/requirements?date=2026-11-03");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task capability_missing_from_snapshot_is_not_found_and_not_cached()
    {
        var service = new Mock<IComplianceApplicationService>();
        service
            .Setup(x => x.GetCapabilityCompliance(It.IsAny<CapabilityId>(), It.IsAny<DateOnly?>()))
            .ThrowsAsync(EntityNotFoundException<Capability>.UsingId("foo"));
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync("/compliance/capabilities/foo?date=2026-11-03");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.CacheControl);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task all_cost_centres_serialises_kind_as_camel_case()
    {
        var service = new Mock<IComplianceApplicationService>();
        service
            .Setup(x => x.GetAllCostCentresCompliance(It.IsAny<DateOnly?>()))
            .ReturnsAsync(
                new AllCostCentresComplianceResult
                {
                    Items =
                    {
                        new CostCentreComplianceResult
                        {
                            CostCentre = "ti-arch",
                            Kind = ComplianceCostCentreKind.CostCentre,
                        },
                        new CostCentreComplianceResult { CostCentre = "rogue", Kind = ComplianceCostCentreKind.Rogue },
                    },
                }
            );
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync("/compliance/cost-centres");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var kinds = document
            .RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(x => x.GetProperty("kind").GetString())
            .ToArray();
        Assert.Equal(new[] { "costCentre", "rogue" }, kinds);
    }

    [Fact]
    public async Task capability_history_defaults_to_ninety_days_ending_today()
    {
        var service = new Mock<IComplianceApplicationService>();
        service
            .Setup(x => x.GetCapabilityHistory(It.IsAny<CapabilityId>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<CapabilityComplianceHistoryEntry>());
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync("/compliance/capabilities/foo/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        service.Verify(x => x.GetCapabilityHistory(CapabilityId.Parse("foo"), Today.AddDays(-89), Today), Times.Once);
    }

    [Theory]
    [InlineData("/compliance/capabilities/foo/history?days=0")]
    [InlineData("/compliance/capabilities/foo/history?days=1101")]
    [InlineData("/compliance/capabilities/foo/history?days=many")]
    [InlineData("/compliance/history?from=2026-11-05&to=2026-11-01")]
    [InlineData("/compliance/history?from=nope")]
    public async Task history_rejects_invalid_ranges(string url)
    {
        var service = new Mock<IComplianceApplicationService>();
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task history_returns_dates_as_iso_strings()
    {
        var service = new Mock<IComplianceApplicationService>();
        service
            .Setup(x => x.GetHistory(It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync(
                new ComplianceHistoryResult
                {
                    OldestDate = Today.AddDays(-2),
                    Items =
                    {
                        new ComplianceDailySummaryResult
                        {
                            Date = Today.AddDays(-2),
                            CapturedAt = new DateTime(2026, 11, 4, 23, 30, 0, DateTimeKind.Utc),
                        },
                    },
                }
            );
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync("/compliance/history");
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026-11-04", root.GetProperty("oldestDate").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal("2026-11-04", item.GetProperty("date").GetString());
        Assert.Equal("2026-11-04T23:30:00Z", item.GetProperty("capturedAt").GetString());
    }

    [Theory]
    [InlineData("/compliance/cost-centres/ti-platform/history", ComplianceCostCentreKind.CostCentre, "ti-platform")]
    [InlineData("/compliance/rogue-capabilities/history", ComplianceCostCentreKind.Rogue, null)]
    [InlineData("/compliance/orphaned-capabilities/history", ComplianceCostCentreKind.Orphaned, null)]
    public async Task cost_centre_history_routes_select_the_group(
        string url,
        ComplianceCostCentreKind kind,
        string? costCentre
    )
    {
        var service = new Mock<IComplianceApplicationService>();
        service
            .Setup(x => x.GetCostCentreHistory(kind, costCentre, null, null))
            .ReturnsAsync(
                new CostCentreComplianceHistoryResult
                {
                    OldestDate = Today,
                    Items =
                    {
                        new CostCentreComplianceHistoryEntry { Date = Today, TotalCapabilities = 2 },
                    },
                }
            );
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync(url);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, root.GetProperty("items")[0].GetProperty("totalCapabilities").GetInt32());
    }

    [Theory]
    [InlineData("/compliance/cost-centres/ti-platform/history?from=2026-11-05&to=2026-11-01")]
    [InlineData("/compliance/rogue-capabilities/history?to=nope")]
    [InlineData("/compliance/requirements/tags/history?from=nope")]
    public async Task history_slices_reject_invalid_ranges(string url)
    {
        await using var application = CreateApplication(new Mock<IComplianceApplicationService>());
        using var client = application.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task requirement_history_of_unknown_requirement_is_not_found()
    {
        var service = new Mock<IComplianceApplicationService>();
        service.Setup(x => x.GetRequirementHistory("nope", null, null)).ThrowsAsync(new KeyNotFoundException());
        await using var application = CreateApplication(service);
        using var client = application.CreateClient();

        var response = await client.GetAsync("/compliance/requirements/nope/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task openapi_document_describes_every_compliance_operation()
    {
        await using var application = CreateApplication(new Mock<IComplianceApplicationService>());
        using var client = application.CreateClient();

        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        var operations = JsonDocument
            .Parse(json)
            .RootElement.GetProperty("paths")
            .EnumerateObject()
            .Where(path => path.Name.StartsWith("/compliance"))
            .SelectMany(path => path.Value.EnumerateObject().Select(op => (Path: path.Name, Operation: op.Value)))
            .ToList();

        Assert.Equal(17, operations.Count);
        Assert.Equal(17, operations.Select(x => x.Operation.GetProperty("operationId").GetString()).Distinct().Count());

        foreach (var (path, operation) in operations)
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()), path);
            Assert.StartsWith("Compliance", operation.GetProperty("operationId").GetString());

            var responses = operation.GetProperty("responses");
            Assert.True(responses.GetProperty("200").TryGetProperty("content", out _), path);
            Assert.True(responses.TryGetProperty("400", out _), path);

            foreach (var parameter in operation.GetProperty("parameters").EnumerateArray())
            {
                var name = parameter.GetProperty("name").GetString();
                Assert.False(
                    string.IsNullOrWhiteSpace(parameter.GetProperty("description").GetString()),
                    $"{path} {name}"
                );

                var schema = parameter.GetProperty("schema");
                Assert.False(schema.TryGetProperty("$ref", out _), $"{path} {name} is not a scalar");
                if (name is "date" or "from" or "to")
                {
                    Assert.Equal("string", schema.GetProperty("type").GetString());
                    Assert.Equal("date", schema.GetProperty("format").GetString());
                }
            }
        }
    }
}
