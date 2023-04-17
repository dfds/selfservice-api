using System.Text.Json;

namespace SelfService.Tests;

public static class AssertJson
{
    public static JsonElement? SelectElement(this JsonDocument document, string path)
    {
        var segments = path
            .Trim('/')
            .Split("/");

        var currentElement = document.RootElement;
        var traveledPath = "/";

        foreach (var segment in segments)
        {
            if (!currentElement.TryGetProperty(segment, out var nestedElement))
            {
                throw new Exception($"No element found at path \"{traveledPath}{segment}\"");
            }

            currentElement = nestedElement;
            traveledPath += segment + "/";
        }

        return currentElement;
    }
}