using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application
{
    // Consolidated: Use RequirementsMetric from Infrastructure.Persistence.Models

    public class RequirementsMetricService : IRequirementsMetricService
    {
        private readonly Infrastructure.Persistence.RequirementsDbContext _requirementsDbContext;
        private readonly ICapabilityRepository _capabilityRepository;

        private static readonly string[] MandatoryTags =
        {
            "dfds.cost.centre",
            "dfds.businessCapability",
            "dfds.env",
            "dfds.data.classification",
            "dfds.service.criticality",
            "dfds.service.availability",
        };

        public RequirementsMetricService(
            Infrastructure.Persistence.RequirementsDbContext requirementsDbContext,
            ICapabilityRepository capabilityRepository
        )
        {
            _requirementsDbContext = requirementsDbContext;
            _capabilityRepository = capabilityRepository;
        }

        public async Task<(
            double totalScore,
            List<Infrastructure.Persistence.Models.RequirementsMetric> scores
        )> GetRequirementScoreAsync(string capabilityId)
        {
            var scores = await _requirementsDbContext
                .Metrics.Where(x => x.CapabilityRootId == capabilityId)
                .Where(x => x.Measurement == "score")
                .ToListAsync();

            // Remove any existing mandatory_tags metric from database results
            scores.RemoveAll(x => x.RequirementId == "mandatory_tags");

            // Calculate mandatory tags score from capability metadata
            var mandatoryTagsMetric = await CalculateMandatoryTagsScore(capabilityId);
            if (mandatoryTagsMetric != null)
            {
                scores.Add(mandatoryTagsMetric);
            }

            double totalScore = scores.Count > 0 ? scores.Average(r => r.Value) : 100;
            return (totalScore, scores);
        }

        private async Task<Infrastructure.Persistence.Models.RequirementsMetric?> CalculateMandatoryTagsScore(
            string capabilityId
        )
        {
            try
            {
                // Try to parse the capability ID
                if (!CapabilityId.TryParse(capabilityId, out var capId))
                {
                    return null;
                }

                // Get the capability to access its metadata
                var capability = await _capabilityRepository.FindBy(capId);
                if (capability == null)
                {
                    return null;
                }

                // Parse the JSON metadata
                JsonObject? jsonObject = null;
                if (!string.IsNullOrEmpty(capability.JsonMetadata))
                {
                    jsonObject = JsonNode.Parse(capability.JsonMetadata)?.AsObject();
                }

                // Count how many tags are present
                var presentCount = 0;
                foreach (var tag in MandatoryTags)
                {
                    var value = jsonObject?[tag]?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        presentCount++;
                    }
                }

                // Calculate score as percentage
                var score = (double)presentCount / MandatoryTags.Length * 100;

                // Create the metric object
                return new Infrastructure.Persistence.Models.RequirementsMetric
                {
                    Id = 0, // Will be auto-generated if saved to DB
                    Name = "dfds_requirement_score",
                    CapabilityRootId = capabilityId,
                    RequirementId = "mandatory_tags",
                    Measurement = "score",
                    HelpUrl = "https://wiki.dfds.cloud/en/playbooks/requirements/Mandatory-tags-for-capabilities",
                    Owner = "platform",
                    Description = "Tracks compliance with DFDS mandatory tagging policy on capabilities",
                    ClusterName = "hellman",
                    Value = score,
                    Help = "Calculated score/percentage for DFDS platform requirements (0-100 scale)",
                    DisplayName = "Use of Mandatory Tags",
                    Type = "gauge",
                    Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    UpdatedAt = DateTime.UtcNow,
                    Labels = new JsonObject(),
                };
            }
            catch
            {
                // If anything goes wrong, just don't add the calculated metric
                return null;
            }
        }

        public async Task<Dictionary<string, double>> GetAllRequirementScoresAsync()
        {
            var metrics = await _requirementsDbContext.Metrics.Where(x => x.Measurement == "score").ToListAsync();

            return metrics.GroupBy(m => m.CapabilityRootId).ToDictionary(g => g.Key, g => g.Average(m => m.Value));
        }
    }

    // Stub implementation for when RequirementsDbContext is not available
    public class StubRequirementsMetricService : IRequirementsMetricService
    {
        private readonly ICapabilityRepository _capabilityRepository;

        private static readonly string[] MandatoryTags =
        {
            "dfds.cost.centre",
            "dfds.businessCapability",
            "dfds.env",
            "dfds.data.classification",
            "dfds.service.criticality",
            "dfds.service.availability",
        };

        public StubRequirementsMetricService(ICapabilityRepository capabilityRepository)
        {
            _capabilityRepository = capabilityRepository;
        }

        public async Task<(
            double totalScore,
            List<Infrastructure.Persistence.Models.RequirementsMetric> scores
        )> GetRequirementScoreAsync(string capabilityId)
        {
            var scores = new List<Infrastructure.Persistence.Models.RequirementsMetric>();

            // Calculate mandatory tags score from capability metadata
            var mandatoryTagsMetric = await CalculateMandatoryTagsScore(capabilityId);
            if (mandatoryTagsMetric != null)
            {
                scores.Add(mandatoryTagsMetric);
            }

            double totalScore = scores.Count > 0 ? scores.Average(r => r.Value) : 100;
            return (totalScore, scores);
        }

        private async Task<Infrastructure.Persistence.Models.RequirementsMetric?> CalculateMandatoryTagsScore(
            string capabilityId
        )
        {
            try
            {
                // Try to parse the capability ID
                if (!CapabilityId.TryParse(capabilityId, out var capId))
                {
                    return null;
                }

                // Get the capability to access its metadata
                var capability = await _capabilityRepository.FindBy(capId);
                if (capability == null)
                {
                    return null;
                }

                // Parse the JSON metadata
                JsonObject? jsonObject = null;
                if (!string.IsNullOrEmpty(capability.JsonMetadata))
                {
                    jsonObject = JsonNode.Parse(capability.JsonMetadata)?.AsObject();
                }

                // Count how many tags are present
                var presentCount = 0;
                foreach (var tag in MandatoryTags)
                {
                    var value = jsonObject?[tag]?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        presentCount++;
                    }
                }

                // Calculate score as percentage
                var score = (double)presentCount / MandatoryTags.Length * 100;

                // Create the metric object
                return new Infrastructure.Persistence.Models.RequirementsMetric
                {
                    Id = 0,
                    Name = "dfds_requirement_score",
                    CapabilityRootId = capabilityId,
                    RequirementId = "mandatory_tags",
                    Measurement = "score",
                    HelpUrl = "https://wiki.dfds.cloud/en/playbooks/requirements/Mandatory-tags-for-capabilities",
                    Owner = "platform",
                    Description = "Tracks compliance with DFDS mandatory tagging policy on capabilities",
                    ClusterName = "hellman",
                    Value = score,
                    Help = "Calculated score/percentage for DFDS platform requirements (0-100 scale)",
                    DisplayName = "Use of Mandatory Tags",
                    Type = "gauge",
                    Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    UpdatedAt = DateTime.UtcNow,
                    Labels = new JsonObject(),
                };
            }
            catch
            {
                // If anything goes wrong, just don't add the calculated metric
                return null;
            }
        }

        public Task<Dictionary<string, double>> GetAllRequirementScoresAsync() =>
            Task.FromResult(new Dictionary<string, double>());
    }
}
