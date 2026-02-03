using System.Diagnostics;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Metrics;
using SelfService.Infrastructure.Api.System;

namespace SelfService.Domain.Services;

public class MetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly IAadAwsSyncCapabilityQuery _aadAwsSyncCapabilityQuery;
    private readonly ICapabilityRepository _capabilityRepository;
    
    private const string CostCentreKey = "dfds.cost.centre";
    private const string OwnerKey = "dfds.owner";
    private const string PlannedSunsetKey = "dfds.planned_sunset";
    private const string DataClassificationKey = "dfds.data.classification";
    private const string ServiceCriticalityKey = "dfds.service.criticality";
    private const string ServiceAvailabilityKey = "dfds.service.availability";

    public MetricsService(
        ILogger<MetricsService> logger,
        IAadAwsSyncCapabilityQuery aadAwsSyncCapabilityQuery,
        ICapabilityRepository capabilityRepository
    )
    {
        _logger = logger;
        _aadAwsSyncCapabilityQuery = aadAwsSyncCapabilityQuery;
        _capabilityRepository = capabilityRepository;
    }

    private string GetJsonMetadataValue(Dictionary<string, JsonElement>? jsonMetadataDeserialised, string key)
    {
        return jsonMetadataDeserialised != null && jsonMetadataDeserialised.ContainsKey(key)
            ? jsonMetadataDeserialised[key].GetString()!
            : "";
    }

    public async Task UpdateMetrics()
    {
        var capabilities = await _aadAwsSyncCapabilityQuery.GetCapabilities(); // TODO: Eventually update aadAwsSyncCapabilityQuery to include json metadata so the additional call to _capabilityRepository won't be necessary. Adding the json is not a lot of work, but testing that it doesn't break the depending services is a bit more. Currently the background job runs every 10 minutes, so I'm not too worried about the lost efficiency here at our scale... yet.
        var capabilitiesNew = await _capabilityRepository.GetAll();
        var capabilitiesMapped = capabilitiesNew.ToDictionary(obj => obj.Id.ToString());

        foreach (var capability in capabilities)
        {
            var context = capability.Contexts.FirstOrDefault();
            var jsonMetadata = capabilitiesMapped[capability.Id].JsonMetadata;
            var jsonMetadataDeserialised = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonMetadata);

            var costCentre = GetJsonMetadataValue(jsonMetadataDeserialised, CostCentreKey);
            var owner = GetJsonMetadataValue(jsonMetadataDeserialised, OwnerKey);
            var plannedSunset = GetJsonMetadataValue(jsonMetadataDeserialised, PlannedSunsetKey);
            var dataClassification = GetJsonMetadataValue(jsonMetadataDeserialised, DataClassificationKey);
            var serviceCriticality = GetJsonMetadataValue(jsonMetadataDeserialised, ServiceCriticalityKey);
            var serviceAvailability = GetJsonMetadataValue(jsonMetadataDeserialised, ServiceAvailabilityKey);

            CapabilityMetrics
                .CapabilityMetric.WithLabels(
                    capability.Name,
                    capability.Id,
                    context?.AWSAccountId ?? "",
                    costCentre,
                    owner,
                    plannedSunset,
                    dataClassification,
                    serviceCriticality,
                    serviceAvailability
                )
                .Set(1);
            
            var tags = new TagList
            {
                { "name", capability.Name },
                { "id", capability.Id }
            };
            
            tags.AddIfPresent("aws_account_id", context?.AWSAccountId);
            tags.AddIfPresent("cost_centre", costCentre);
            tags.AddIfPresent("owner", owner);
            tags.AddIfPresent("planned_sunset", plannedSunset);
            tags.AddIfPresent("data_classification", dataClassification);
            tags.AddIfPresent("service_criticality", serviceCriticality);
            tags.AddIfPresent("service_availability", serviceAvailability);

            // CapabilityMetrics.CapabilityMetricOtel.Record(1, tags);
        }
    }
}
