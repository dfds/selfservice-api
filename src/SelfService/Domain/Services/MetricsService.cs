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


    public MetricsService(ILogger<MetricsService> logger, IAadAwsSyncCapabilityQuery aadAwsSyncCapabilityQuery, ICapabilityRepository capabilityRepository)
    {
        _logger = logger;
        _aadAwsSyncCapabilityQuery = aadAwsSyncCapabilityQuery;
        _capabilityRepository = capabilityRepository;
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
            var costCentre = jsonMetadataDeserialised != null && jsonMetadataDeserialised.ContainsKey(CostCentreKey) ? jsonMetadataDeserialised[CostCentreKey].GetString()! : "";
            CapabilityMetrics.CapabilityMetric.WithLabels(capability.Name, capability.Id, context?.AWSAccountId ?? "", costCentre).Set(1);
        }
    }
}