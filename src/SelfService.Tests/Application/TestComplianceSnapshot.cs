using System.Text.Json;
using Microsoft.Data.Sqlite;
using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Application;

public class TestComplianceSnapshot
{
    private static readonly DateOnly Day = new(2026, 11, 2);

    private const string AllTags = """
        {
            "dfds.cost.centre": "ti-platform",
            "dfds.businessCapability": "Platform",
            "dfds.env": "production",
            "dfds.data.classification": "internal",
            "dfds.service.criticality": "high",
            "dfds.service.availability": "99.9"
        }
        """;

    private const string SomeTags = """{"dfds.cost.centre": "ti-platform", "dfds.env": "production"}""";

    /// <summary>
    /// The real compliance service over SQLite for both databases, with live inputs the test can change between
    /// captures.
    /// </summary>
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly InMemoryDatabaseFactory _databaseFactory = new();
        private readonly SqliteConnection _requirementsConnection = new("Filename=:memory:");
        private DateTime _now;

        public List<Capability> Capabilities { get; } = new();
        public Dictionary<CapabilityId, int> MemberCounts { get; } = new();
        public HashSet<CapabilityId> KubernetesLinked { get; } = new();
        public SelfServiceDbContext DbContext { get; private set; } = null!;
        public SqliteRequirementsDbContext Requirements { get; private set; } = null!;
        public ComplianceSnapshotRepository Repository { get; private set; } = null!;
        public IComplianceApplicationService Service { get; private set; } = null!;

        public static async Task<Fixture> Create()
        {
            var fixture = new Fixture();
            await fixture.Initialize();
            return fixture;
        }

        private async Task Initialize()
        {
            DbContext = await _databaseFactory.CreateSelfServiceDbContext();
            await _requirementsConnection.OpenAsync();
            Requirements = await SqliteRequirementsDbContext.Create(_requirementsConnection);
            Repository = new ComplianceSnapshotRepository(DbContext);
            SetToday(Day);

            var capabilityRepository = new Mock<ICapabilityRepository>();
            capabilityRepository
                .Setup(x => x.GetAllActive())
                .ReturnsAsync(() => Capabilities.Where(c => c.Status == CapabilityStatusOptions.Active).ToList());
            capabilityRepository
                .Setup(x => x.FindBy(It.IsAny<CapabilityId>()))
                .ReturnsAsync((CapabilityId id) => Capabilities.FirstOrDefault(c => c.Id == id));

            var membershipRepository = new Mock<IMembershipRepository>();
            membershipRepository
                .Setup(x => x.GetMemberCountsByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
                .ReturnsAsync(
                    (IEnumerable<CapabilityId> ids) =>
                        ids.Distinct().Where(MemberCounts.ContainsKey).ToDictionary(id => id, id => MemberCounts[id])
                );

            var awsAccountRepository = new Mock<IAwsAccountRepository>();
            awsAccountRepository
                .Setup(x => x.GetByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
                .ReturnsAsync(
                    (IEnumerable<CapabilityId> ids) =>
                        ids.Where(KubernetesLinked.Contains).Select(LinkedAccount).ToList()
                );
            awsAccountRepository
                .Setup(x => x.FindBy(It.IsAny<CapabilityId>()))
                .ReturnsAsync((CapabilityId id) => KubernetesLinked.Contains(id) ? LinkedAccount(id) : null);

            Service = A
                .ComplianceApplicationService.WithCapabilityRepository(capabilityRepository.Object)
                .WithMembershipRepository(membershipRepository.Object)
                .WithAwsAccountRepository(awsAccountRepository.Object)
                .WithRequirementsDbContext(Requirements)
                .WithComplianceSnapshotRepository(Repository)
                .WithSystemTime(new SystemTime(() => _now))
                .Build();
        }

        private static AwsAccount LinkedAccount(CapabilityId id)
        {
            var account = AwsAccount.RequestNew(id, DateTime.UtcNow, "test@example.com");
            account.LinkKubernetesNamespace($"ns-{id}", DateTime.UtcNow);
            return account;
        }

        public void SetToday(DateOnly date) => _now = date.ToDateTime(new TimeOnly(15, 30), DateTimeKind.Utc);

        public Capability AddCapability(string id, string metadata, int members = 1, string? name = null)
        {
            var capabilityId = CapabilityId.Parse(id);
            var capability = A.Capability.WithId(capabilityId).WithName(name ?? id).WithJsonMetadata(metadata).Build();
            Capabilities.RemoveAll(c => c.Id == capabilityId);
            Capabilities.Add(capability);
            MemberCounts[capabilityId] = members;
            return capability;
        }

        public async Task SetExternalSecrets(string capabilityId, int compliant, int nonCompliant)
        {
            Requirements.Metrics.RemoveRange(
                Requirements.Metrics.Where(m =>
                    m.CapabilityRootId == capabilityId && m.RequirementId == "external_secrets"
                )
            );
            Requirements.Metrics.AddRange(
                Metric(capabilityId, "compliant_total", compliant),
                Metric(capabilityId, "non_compliant_total", nonCompliant)
            );
            await Requirements.SaveChangesAsync();
        }

        private static RequirementsMetric Metric(string capabilityId, string measurement, double value) =>
            new()
            {
                Name = "dfds_requirement_value",
                CapabilityRootId = capabilityId,
                RequirementId = "external_secrets",
                Measurement = measurement,
                Type = "gauge",
                Value = value,
            };

        /// <summary>What <c>ComplianceSnapshotRecorder</c> does on each run.</summary>
        public async Task Capture()
        {
            var capture = await Service.CaptureCurrent();
            await Repository.ReplaceForDate(capture.Date, capture.CapturedAt, capture.Rows, capture.Summary);
        }

        public async ValueTask DisposeAsync()
        {
            await Requirements.DisposeAsync();
            await _requirementsConnection.DisposeAsync();
            await _databaseFactory.DisposeAsync();
        }
    }

    private static ComplianceStatus CategoryStatus(CapabilityComplianceResult result, string categoryName) =>
        result.Categories.Single(c => c.CategoryName == categoryName).Status;

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task historical_read_returns_captured_state_after_live_inputs_change()
    {
        await using var fixture = await Fixture.Create();
        var capability = fixture.AddCapability("cap-a", AllTags);
        fixture.KubernetesLinked.Add(capability.Id);
        await fixture.SetExternalSecrets("cap-a", compliant: 3, nonCompliant: 0);
        await fixture.Capture();

        fixture.SetToday(Day.AddDays(1));
        fixture.AddCapability("cap-a", SomeTags);
        await fixture.SetExternalSecrets("cap-a", compliant: 1, nonCompliant: 2);

        var historical = await fixture.Service.GetCapabilityCompliance(capability.Id, Day);
        var live = await fixture.Service.GetCapabilityCompliance(capability.Id);

        Assert.Equal(ComplianceStatus.Compliant, CategoryStatus(historical, "Tags"));
        Assert.Equal(ComplianceStatus.Compliant, CategoryStatus(historical, "External Secrets"));
        Assert.Equal(ComplianceStatus.NonCompliant, CategoryStatus(live, "Tags"));
        Assert.Equal(ComplianceStatus.NonCompliant, CategoryStatus(live, "External Secrets"));

        Assert.Equal(1, (await fixture.Service.GetComplianceSummary(Day)).FullyCompliantCapabilities);
        Assert.Equal(0, (await fixture.Service.GetComplianceSummary()).FullyCompliantCapabilities);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task recapturing_the_same_day_replaces_its_rows_and_summary()
    {
        await using var fixture = await Fixture.Create();
        fixture.AddCapability("cap-a", SomeTags);
        fixture.AddCapability("cap-b", SomeTags);
        await fixture.Capture();

        fixture.Capabilities.RemoveAll(c => c.Id.ToString() == "cap-b");
        fixture.AddCapability("cap-a", AllTags);
        await fixture.Capture();

        var row = Assert.Single(await fixture.Repository.GetForDate(Day));
        Assert.Equal("cap-a", row.CapabilityId);
        Assert.Equal(ComplianceStatus.Compliant, row.OverallStatus);

        var summary = Assert.Single((await fixture.Service.GetHistory()).Items);
        Assert.Equal(1, summary.TotalCapabilities);
        Assert.Equal(1, summary.FullyCompliantCapabilities);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task rogue_and_orphaned_are_taken_from_the_snapshot()
    {
        await using var fixture = await Fixture.Create();
        fixture.AddCapability("cap-a", """{"dfds.env": "production"}""", members: 0);
        await fixture.Capture();

        fixture.SetToday(Day.AddDays(1));
        fixture.AddCapability("cap-a", AllTags, members: 3);

        Assert.Equal(1, (await fixture.Service.GetRogueCapabilitiesCompliance(Day)).TotalCapabilities);
        Assert.Equal(1, (await fixture.Service.GetOrphanedCapabilitiesCompliance(Day)).TotalCapabilities);
        Assert.Equal(0, (await fixture.Service.GetCostCentreCompliance("ti-platform", Day)).TotalCapabilities);

        Assert.Equal(0, (await fixture.Service.GetRogueCapabilitiesCompliance()).TotalCapabilities);
        Assert.Equal(0, (await fixture.Service.GetOrphanedCapabilitiesCompliance()).TotalCapabilities);
        Assert.Equal(1, (await fixture.Service.GetCostCentreCompliance("ti-platform")).TotalCapabilities);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task snapshot_reads_match_live_reads_of_the_same_state()
    {
        await using var fixture = await Fixture.Create();
        var linked = fixture.AddCapability("cap-a", AllTags, name: "A");
        fixture.KubernetesLinked.Add(linked.Id);
        await fixture.SetExternalSecrets("cap-a", compliant: 2, nonCompliant: 1);
        fixture.AddCapability("cap-b", """{"dfds.cost.centre": "TI-Platform"}""", name: "B");
        fixture.AddCapability("cap-c", """{"dfds.cost.centre": "ti-ferry"}""", members: 0, name: "C");
        fixture.AddCapability("cap-d", "{}", name: "D");
        await fixture.Capture();

        // Snapshot rows are read back ordered by capability id and live rows in GetAllActive order; the ids
        // are added in sorted order so both reads see the same row order.
        async Task AssertSame<T>(Func<DateOnly?, Task<T>> read) =>
            Assert.Equal(JsonSerializer.Serialize(await read(null)), JsonSerializer.Serialize(await read(Day)));

        await AssertSame(fixture.Service.GetAllCostCentresCompliance);
        await AssertSame(fixture.Service.GetComplianceSummary);
        await AssertSame(fixture.Service.GetRequirementsCompliance);
        await AssertSame(fixture.Service.GetRogueCapabilitiesComplianceDetails);
        await AssertSame(fixture.Service.GetOrphanedCapabilitiesComplianceDetails);
        await AssertSame(d => fixture.Service.GetCostCentreComplianceDetails("ti-platform", d));
        await AssertSame(d => fixture.Service.GetRequirementComplianceDetails("external-secrets", d));
        await AssertSame(d => fixture.Service.GetCapabilityCompliance(linked.Id, d));

        var costCentres = (await fixture.Service.GetAllCostCentresCompliance(Day)).Items;
        Assert.Equal(
            new[] { "ti-ferry", "TI-Platform", "rogue", "orphaned" },
            costCentres.Select(c => c.CostCentre).ToArray()
        );
        Assert.Equal(2, costCentres.Single(c => c.CostCentre == "TI-Platform").TotalCapabilities);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task history_covers_the_requested_range_oldest_first()
    {
        await using var fixture = await Fixture.Create();

        Assert.Null((await fixture.Service.GetHistory()).OldestDate);
        Assert.Empty((await fixture.Service.GetHistory()).Items);

        fixture.AddCapability("cap-a", SomeTags);
        foreach (var date in new[] { Day.AddDays(-1), Day.AddDays(-3), Day })
        {
            fixture.SetToday(date);
            await fixture.Capture();
        }

        var all = await fixture.Service.GetHistory();
        Assert.Equal(Day.AddDays(-3), all.OldestDate);
        Assert.Equal(new[] { Day.AddDays(-3), Day.AddDays(-1), Day }, all.Items.Select(x => x.Date).ToArray());

        var range = await fixture.Service.GetHistory(Day.AddDays(-2), Day.AddDays(-1));
        Assert.Equal(Day.AddDays(-3), range.OldestDate);
        Assert.Equal(new[] { Day.AddDays(-1) }, range.Items.Select(x => x.Date).ToArray());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task history_computes_today_live_and_keeps_past_days_as_captured()
    {
        await using var fixture = await Fixture.Create();
        fixture.AddCapability("cap-a", SomeTags);
        fixture.SetToday(Day.AddDays(-1));
        await fixture.Capture();
        fixture.SetToday(Day);
        await fixture.Capture();
        fixture.AddCapability("cap-a", AllTags);

        var items = (await fixture.Service.GetHistory()).Items;
        Assert.Equal(new[] { Day.AddDays(-1), Day }, items.Select(x => x.Date).ToArray());
        Assert.Equal(new[] { 0, 1 }, items.Select(x => x.FullyCompliantCapabilities).ToArray());
        Assert.Equal(
            (await fixture.Service.GetComplianceSummary()).FullyCompliantCapabilities,
            items[^1].FullyCompliantCapabilities
        );

        fixture.SetToday(Day.AddDays(1));
        var uncaptured = (await fixture.Service.GetHistory()).Items;
        Assert.Equal(new[] { Day.AddDays(-1), Day, Day.AddDays(1) }, uncaptured.Select(x => x.Date).ToArray());

        var past = (await fixture.Service.GetHistory(Day.AddDays(-1), Day)).Items;
        Assert.Equal(new[] { 0, 0 }, past.Select(x => x.FullyCompliantCapabilities).ToArray());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task capability_history_computes_today_live_while_the_capability_is_active()
    {
        await using var fixture = await Fixture.Create();
        var capability = fixture.AddCapability("cap-a", SomeTags);
        await fixture.Capture();
        fixture.AddCapability("cap-a", AllTags);

        var entry = Assert.Single(await fixture.Service.GetCapabilityHistory("cap-a", Day.AddDays(-5), Day));
        Assert.Equal(Day, entry.Date);
        Assert.Equal(ComplianceStatus.Compliant, entry.Row.OverallStatus);

        var deleted = fixture.AddCapability("cap-a", AllTags);
        deleted.Status = CapabilityStatusOptions.Deleted;
        var stored = Assert.Single(await fixture.Service.GetCapabilityHistory(capability.Id, Day.AddDays(-5), Day));
        Assert.Equal(ComplianceStatus.NonCompliant, stored.Row.OverallStatus);

        Assert.Empty(await fixture.Service.GetCapabilityHistory("cap-none", Day.AddDays(-5), Day));
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task capability_history_returns_that_capability_per_day()
    {
        await using var fixture = await Fixture.Create();
        fixture.AddCapability("cap-a", SomeTags);
        fixture.AddCapability("cap-b", AllTags);
        fixture.SetToday(Day.AddDays(-1));
        await fixture.Capture();
        fixture.AddCapability("cap-a", AllTags);
        fixture.SetToday(Day);
        await fixture.Capture();

        var history = await fixture.Service.GetCapabilityHistory("cap-a", Day.AddDays(-5), Day);

        Assert.Equal(new[] { Day.AddDays(-1), Day }, history.Select(x => x.Date).ToArray());
        Assert.All(history, x => Assert.Equal("cap-a", x.Row.CapabilityId));
        Assert.Equal(
            new[] { ComplianceStatus.NonCompliant, ComplianceStatus.Compliant },
            history.Select(x => x.Row.OverallStatus).ToArray()
        );
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task day_without_snapshot_and_capability_missing_from_snapshot_are_different_errors()
    {
        await using var fixture = await Fixture.Create();
        fixture.AddCapability("cap-a", SomeTags);
        await fixture.Capture();
        fixture.SetToday(Day.AddDays(1));
        fixture.AddCapability("cap-new", SomeTags);

        var missingDay = await Assert.ThrowsAsync<ComplianceSnapshotNotFoundException>(
            () => fixture.Service.GetComplianceSummary(Day.AddDays(-1))
        );
        Assert.Equal(Day.AddDays(-1), missingDay.Date);
        await Assert.ThrowsAsync<ComplianceSnapshotNotFoundException>(
            () => fixture.Service.GetCapabilityCompliance("cap-a", Day.AddDays(-1))
        );
        await Assert.ThrowsAsync<EntityNotFoundException<Capability>>(
            () => fixture.Service.GetCapabilityCompliance("cap-new", Day)
        );
    }

    private const string AllTagsUpperCaseCostCentre = """
        {
            "dfds.cost.centre": "TI-PLATFORM",
            "dfds.businessCapability": "Platform",
            "dfds.env": "production",
            "dfds.data.classification": "internal",
            "dfds.service.criticality": "high",
            "dfds.service.availability": "99.9"
        }
        """;

    private static async Task<Fixture> CaptureTwoDaysOfGroups()
    {
        var fixture = await Fixture.Create();
        fixture.AddCapability("cap-a", SomeTags);
        fixture.AddCapability("cap-b", AllTagsUpperCaseCostCentre);
        fixture.AddCapability("cap-rogue", "{}", members: 0);
        foreach (var date in new[] { Day.AddDays(-1), Day })
        {
            fixture.SetToday(date);
            await fixture.Capture();
        }
        return fixture;
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task cost_centre_history_has_that_groups_totals_per_requirement()
    {
        await using var fixture = await CaptureTwoDaysOfGroups();

        var history = await fixture.Service.GetCostCentreHistory(ComplianceCostCentreKind.CostCentre, "Ti-Platform");

        Assert.Equal(Day.AddDays(-1), history.OldestDate);
        Assert.Equal(new[] { Day.AddDays(-1), Day }, history.Items.Select(x => x.Date).ToArray());
        var item = history.Items[1];
        Assert.Equal((2, 1, 1), (item.TotalCapabilities, item.CompliantCount, item.NonCompliantCount));
        var tags = item.Requirements.Single(r => r.RequirementId == "tags");
        Assert.Equal((2, 1, 1), (tags.TotalCapabilities, tags.CompliantCount, tags.NonCompliantCount));
        Assert.Equal(0, item.Requirements.Single(r => r.RequirementId == "external-secrets").TotalCapabilities);

        var rogue = await fixture.Service.GetCostCentreHistory(ComplianceCostCentreKind.Rogue, null);
        Assert.All(rogue.Items, x => Assert.Equal((1, 0), (x.TotalCapabilities, x.CompliantCount)));
        var orphaned = await fixture.Service.GetCostCentreHistory(ComplianceCostCentreKind.Orphaned, "ignored");
        Assert.All(orphaned.Items, x => Assert.Equal(1, x.TotalCapabilities));

        var unknown = await fixture.Service.GetCostCentreHistory(ComplianceCostCentreKind.CostCentre, "nope");
        Assert.Empty(unknown.Items);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task requirement_history_has_that_requirements_totals_per_cost_centre()
    {
        await using var fixture = await CaptureTwoDaysOfGroups();

        var history = await fixture.Service.GetRequirementHistory("TAGS");

        Assert.Equal("tags", history.RequirementId);
        Assert.Equal(2, history.Items.Count);
        var item = history.Items[1];
        Assert.Equal((3, 1), (item.Totals.TotalCapabilities, item.Totals.CompliantCount));
        Assert.Equal(
            new[]
            {
                (ComplianceCostCentreKind.CostCentre, (string?)"TI-PLATFORM", 2, 1),
                (ComplianceCostCentreKind.Rogue, null, 1, 0),
                (ComplianceCostCentreKind.Orphaned, null, 1, 0),
            },
            item.CostCentres.Select(c => (c.Kind, c.CostCentre, c.TotalCapabilities, c.CompliantCount)).ToArray()
        );
        Assert.All(item.CostCentres, c => Assert.Empty(c.Requirements));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.Service.GetRequirementHistory("nope"));
    }

    [Fact]
    public async Task stub_service_has_no_history()
    {
        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository.Setup(x => x.GetAllActive()).ReturnsAsync(new List<Capability>());
        var service = A.ComplianceApplicationService.WithCapabilityRepository(capabilityRepository.Object).Build();

        Assert.IsType<StubComplianceApplicationService>(service);
        await Assert.ThrowsAsync<ComplianceSnapshotNotFoundException>(() => service.GetComplianceSummary(Day));
        await Assert.ThrowsAsync<ComplianceSnapshotNotFoundException>(
            () => service.GetCapabilityCompliance("cap-a", Day)
        );
        Assert.Null((await service.GetHistory()).OldestDate);
        Assert.Empty(await service.GetCapabilityHistory("cap-a", Day.AddDays(-1), Day));
        Assert.Empty((await service.GetCostCentreHistory(ComplianceCostCentreKind.Rogue, null)).Items);
        Assert.Equal("tags", (await service.GetRequirementHistory("tags")).RequirementId);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetRequirementHistory("nope"));
    }
}
