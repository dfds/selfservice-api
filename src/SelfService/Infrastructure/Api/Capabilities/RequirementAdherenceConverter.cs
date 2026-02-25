using System.Collections.Generic;
using Json.Schema;
using SelfService.Application;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Capabilities
{
    public static class RequirementsMetricConverter
    {
        public static RequirementsMetricApiResource Convert(
            string capabilityId,
            double totalScore,
            List<Infrastructure.Persistence.Models.RequirementsMetric> metrics
        )
        {
            var requirementsMetrics = new List<RequirementsMetricDetail>();
            foreach (var metric in metrics)
            {
                // Handle invalid Unix timestamps - valid range is -62135596800 to 253402300799
                DateTime dateValue;
                try
                {
                    if (metric.Date >= -62135596800 && metric.Date <= 253402300799)
                    {
                        dateValue = DateTimeOffset.FromUnixTimeSeconds(metric.Date).DateTime;
                    }
                    else
                    {
                        dateValue = DateTime.UtcNow;
                    }
                }
                catch
                {
                    dateValue = DateTime.UtcNow;
                }

                requirementsMetrics.Add(
                    new RequirementsMetricDetail
                    {
                        Id = metric.Id,
                        Name = metric.Name,
                        CapabilityRootId = metric.CapabilityRootId,
                        RequirementId = metric.RequirementId,
                        Measurement = metric.Measurement,
                        HelpUrl = metric.HelpUrl,
                        Owner = metric.Owner,
                        Description = metric.Description,
                        ClusterName = metric.ClusterName,
                        Value = metric.Value,
                        Help = metric.Help,
                        Type = metric.Type,
                        Date = dateValue,
                        UpdatedAt = metric.UpdatedAt,
                        Labels = metric.Labels,
                    }
                );
            }
            return new RequirementsMetricApiResource
            {
                CapabilityId = capabilityId,
                TotalScore = totalScore,
                RequirementsMetrics = requirementsMetrics,
            };
        }
    }
}
