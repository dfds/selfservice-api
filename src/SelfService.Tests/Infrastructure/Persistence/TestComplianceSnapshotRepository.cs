using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestComplianceSnapshotRepository
{
    private static readonly DateOnly Today = new(2026, 11, 6);

    private static DateTime CapturedAt(DateOnly date) => date.ToDateTime(new TimeOnly(23, 10), DateTimeKind.Utc);

    private static CapabilityComplianceRow Row(
        string capabilityId,
        ComplianceStatus status = ComplianceStatus.NonCompliant,
        string? costCentre = "ti-platform",
        int memberCount = 2
    ) =>
        new(
            CapabilityId: capabilityId,
            CapabilityName: $"Name of {capabilityId}",
            JsonMetadata: costCentre == null ? "{}" : $$"""{"dfds.cost.centre":"{{costCentre}}"}""",
            CostCentre: costCentre,
            MemberCount: memberCount,
            OverallStatus: status,
            Categories: new List<ComplianceCategoryResult>
            {
                new()
                {
                    CategoryName = "Tags",
                    Status = status,
                    Score = 50,
                    HelpUrl = "https://example.com/tags",
                    Items =
                    {
                        new ComplianceCategoryItem
                        {
                            Name = "dfds.env",
                            Status = "missing",
                            Detail = null,
                        },
                    },
                },
                new()
                {
                    CategoryName = "IRSA Mutual Trust",
                    Status = ComplianceStatus.NonCompliant,
                    NonCompliantItems = { "sa-a", "sa-b" },
                },
            }
        );

    private static ComplianceDailySummaryResult Summary(DateOnly date, int total) =>
        new()
        {
            Date = date,
            CapturedAt = CapturedAt(date),
            TotalCapabilities = total,
            NonCompliantCapabilities = total,
            Requirements =
            {
                new ComplianceRequirementTotals
                {
                    RequirementId = "tags",
                    CategoryName = "Tags",
                    DisplayName = "Tags for Capabilities",
                    TotalCapabilities = total,
                    NonCompliantCount = total,
                },
            },
            CostCentres =
            {
                new ComplianceCostCentreTotals
                {
                    CostCentre = "ti-platform",
                    Kind = ComplianceCostCentreKind.CostCentre,
                    TotalCapabilities = total,
                },
                new ComplianceCostCentreTotals { Kind = ComplianceCostCentreKind.Rogue },
            },
        };

    private static Task Replace(
        ComplianceSnapshotRepository repository,
        DateOnly date,
        params CapabilityComplianceRow[] rows
    ) => repository.ReplaceForDate(date, CapturedAt(date), rows, Summary(date, rows.Length));

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task replace_for_date_round_trips_rows_and_summary()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = new ComplianceSnapshotRepository(dbContext);

        await Replace(
            sut,
            Today,
            Row("cap-a"),
            Row("cap-b", ComplianceStatus.Compliant, costCentre: null, memberCount: 0)
        );

        var rows = await sut.GetForDate(Today);
        Assert.Equal(new[] { "cap-a", "cap-b" }, rows.Select(r => r.CapabilityId).ToArray());
        var rogue = rows[1];
        Assert.Null(rogue.CostCentre);
        Assert.Equal(0, rogue.MemberCount);
        Assert.Equal(ComplianceStatus.Compliant, rogue.OverallStatus);
        Assert.Equal("{}", rogue.JsonMetadata);

        var tags = rows[0].Categories[0];
        Assert.Equal(ComplianceStatus.NonCompliant, tags.Status);
        Assert.Equal(50, tags.Score);
        Assert.Equal("https://example.com/tags", tags.HelpUrl);
        var item = Assert.Single(tags.Items);
        Assert.Equal(("dfds.env", "missing", (string?)null), (item.Name, item.Status, item.Detail));
        Assert.Equal(new[] { "sa-a", "sa-b" }, rows[0].Categories[1].NonCompliantItems);

        var summary = Assert.Single(await sut.GetSummaries(Today, Today));
        Assert.Equal(Today, summary.Date);
        Assert.Equal(CapturedAt(Today), summary.CapturedAt);
        Assert.Equal(DateTimeKind.Utc, summary.CapturedAt.Kind);
        Assert.Equal(2, summary.TotalCapabilities);
        Assert.Equal("Tags for Capabilities", Assert.Single(summary.Requirements).DisplayName);
        Assert.Equal(
            new[] { ComplianceCostCentreKind.CostCentre, ComplianceCostCentreKind.Rogue },
            summary.CostCentres.Select(c => c.Kind).ToArray()
        );
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task json_columns_use_camel_case_names_and_enum_values()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = new ComplianceSnapshotRepository(dbContext);

        await Replace(sut, Today, Row("cap-a"));

        // db/seed/generate-compliance-history.py writes this format.
        var snapshot = await dbContext.ComplianceSnapshots.AsNoTracking().SingleAsync();
        Assert.Contains("\"categoryName\":\"Tags\"", snapshot.Categories);
        Assert.Contains("\"status\":\"nonCompliant\"", snapshot.Categories);
        var summary = await dbContext.ComplianceDailySummaries.AsNoTracking().SingleAsync();
        Assert.Contains("\"kind\":\"costCentre\"", summary.CostCentres);
        Assert.Contains("\"requirementId\":\"tags\"", summary.Requirements);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task replacing_a_date_twice_on_one_context_keeps_only_the_second_capture()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = new ComplianceSnapshotRepository(dbContext);

        await Replace(sut, Today, Row("cap-a"), Row("cap-b"));
        await Replace(sut, Today, Row("cap-a", ComplianceStatus.Compliant));

        var row = Assert.Single(await sut.GetForDate(Today));
        Assert.Equal(ComplianceStatus.Compliant, row.OverallStatus);
        Assert.Equal(1, Assert.Single(await sut.GetSummaries(Today, Today)).TotalCapabilities);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task replacing_a_date_leaves_other_dates_alone()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = new ComplianceSnapshotRepository(dbContext);

        await Replace(sut, Today.AddDays(-1), Row("cap-a"), Row("cap-b"));
        await Replace(sut, Today, Row("cap-a"));
        await Replace(sut, Today, Row("cap-c"));

        Assert.Equal(2, (await sut.GetForDate(Today.AddDays(-1))).Count);
        Assert.Equal("cap-c", Assert.Single(await sut.GetForDate(Today)).CapabilityId);
        Assert.Empty(await sut.GetForDate(Today.AddDays(-2)));
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task summaries_and_capability_history_are_inclusive_and_oldest_first()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = new ComplianceSnapshotRepository(dbContext);

        Assert.Null(await sut.GetOldestDate());

        foreach (var offset in new[] { 0, -4, -2, -1 })
        {
            await Replace(sut, Today.AddDays(offset), Row("cap-a"), Row("cap-b"));
        }

        Assert.Equal(Today.AddDays(-4), await sut.GetOldestDate());
        Assert.Equal(
            new[] { Today.AddDays(-2), Today.AddDays(-1) },
            (await sut.GetSummaries(Today.AddDays(-3), Today.AddDays(-1))).Select(s => s.Date).ToArray()
        );

        var history = await sut.GetCapabilityHistory("cap-b", Today.AddDays(-2), Today);
        Assert.Equal(new[] { Today.AddDays(-2), Today.AddDays(-1), Today }, history.Select(h => h.Date).ToArray());
        Assert.All(history, h => Assert.Equal("cap-b", h.Row.CapabilityId));
        Assert.Empty(await sut.GetCapabilityHistory("cap-unknown", Today.AddDays(-4), Today));
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task delete_older_than_retention_keeps_the_oldest_retained_day()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = new ComplianceSnapshotRepository(dbContext);

        var cutoff = ComplianceRetention.OldestRetained(Today);
        Assert.Equal(Today.AddDays(-1100), cutoff);
        await Replace(sut, cutoff.AddDays(-1), Row("cap-a"));
        await Replace(sut, cutoff, Row("cap-a"));
        await Replace(sut, Today, Row("cap-a"));

        await sut.DeleteOlderThan(cutoff);

        Assert.Empty(await sut.GetForDate(cutoff.AddDays(-1)));
        Assert.Single(await sut.GetForDate(cutoff));
        Assert.Equal(cutoff, await sut.GetOldestDate());
        Assert.Equal(
            new[] { cutoff, Today },
            (await sut.GetSummaries(DateOnly.MinValue, DateOnly.MaxValue)).Select(s => s.Date).ToArray()
        );
    }
}
