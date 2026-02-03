using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.System;

namespace SelfService.Infrastructure.Api.Metrics;


public class CapabilityMetrics
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ObservableGauge<long> _capabilityMetricOtelObservable;

    public static readonly Meter SelfServiceMeter = new("selfserviceapi");
    private List<Measurement<long>> _cachedCapabilitiesMeasurements = new List<Measurement<long>>();
    
    private const string CostCentreKey = "dfds.cost.centre";
    private const string OwnerKey = "dfds.owner";
    private const string PlannedSunsetKey = "dfds.planned_sunset";
    private const string DataClassificationKey = "dfds.data.classification";
    private const string ServiceCriticalityKey = "dfds.service.criticality";
    private const string ServiceAvailabilityKey = "dfds.service.availability";
    
    public CapabilityMetrics(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        
        _capabilityMetricOtelObservable = SelfServiceMeter.CreateObservableGauge<long>("selfservice_capability_observable", () => { return ObserveCapability(); }, description: "Capability data");
    }
    
    private string GetJsonMetadataValue(Dictionary<string, JsonElement>? jsonMetadataDeserialised, string key)
    {
        return jsonMetadataDeserialised != null && jsonMetadataDeserialised.ContainsKey(key)
            ? jsonMetadataDeserialised[key].GetString()!
            : "";
    }
    
    public async Task UpdateCapabilityCache()
    {
        using var scope = _scopeFactory.CreateScope();
        var aadAwsSyncCapabilityQuery = scope.ServiceProvider.GetRequiredService<IAadAwsSyncCapabilityQuery>();
        var capabilityRepository = scope.ServiceProvider.GetRequiredService<ICapabilityRepository>();
         
        var payload = new List<Measurement<long>>();
        var capabilities = await aadAwsSyncCapabilityQuery.GetCapabilities(); // TODO: Eventually update aadAwsSyncCapabilityQuery to include json metadata so the additional call to _capabilityRepository won't be necessary. Adding the json is not a lot of work, but testing that it doesn't break the depending services is a bit more. Currently the background job runs every 10 minutes, so I'm not too worried about the lost efficiency here at our scale... yet.

        foreach (var capability in capabilities)
        {
            var context = capability.Contexts.FirstOrDefault();
            var jsonMetadataDeserialised = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(capability.JsonMetadata);

            var costCentre = GetJsonMetadataValue(jsonMetadataDeserialised, CostCentreKey);
            var owner = GetJsonMetadataValue(jsonMetadataDeserialised, OwnerKey);
            var plannedSunset = GetJsonMetadataValue(jsonMetadataDeserialised, PlannedSunsetKey);
            var dataClassification = GetJsonMetadataValue(jsonMetadataDeserialised, DataClassificationKey);
            var serviceCriticality = GetJsonMetadataValue(jsonMetadataDeserialised, ServiceCriticalityKey);
            var serviceAvailability = GetJsonMetadataValue(jsonMetadataDeserialised, ServiceAvailabilityKey);
                
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

            var measurement = new Measurement<long>(1, tags);
            payload.Add(measurement);
        }

        _cachedCapabilitiesMeasurements = payload;
    }

    private IEnumerable<Measurement<long>> ObserveCapability()
    {
        return _cachedCapabilitiesMeasurements;
    }
}

public static class TagExtensions
{
    public static void AddIfPresent(this ref TagList tags, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            tags.Add(key, value);
        }
    }
}