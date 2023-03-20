﻿using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public abstract class ResourceDtoBase
{
    [JsonPropertyName("_links")]
    public Dictionary<string, ResourceLink> Links { get; set; } = new();
}