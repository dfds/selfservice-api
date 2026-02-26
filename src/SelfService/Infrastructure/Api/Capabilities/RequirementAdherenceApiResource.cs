using System.Collections.Generic;

namespace SelfService.Infrastructure.Api.Capabilities
{
    public class RequirementsMetricApiResource
    {
        public required string CapabilityId { get; set; }
        public double TotalScore { get; set; }
        public required List<RequirementsMetricDetail> RequirementsMetrics { get; set; }
    }

    public class RequirementsMetricDetail
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string CapabilityRootId { get; set; } = null!;
        public string RequirementId { get; set; } = null!;
        public string Measurement { get; set; } = null!;
        public string? HelpUrl { get; set; }
        public string? Owner { get; set; }
        public string? Description { get; set; }
        public string? ClusterName { get; set; }
        public double Value { get; set; }
        public string? Help { get; set; }
        public string? DisplayName { get; set; }
        public string? Type { get; set; }
        public DateTime Date { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public object? Labels { get; set; }
    }
}
