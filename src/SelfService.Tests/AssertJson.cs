using System.Text.Json;

namespace SelfService.Tests;

public static class AssertJson
{
    public static JsonElement? SelectElement(this JsonDocument document, string path) =>
        SelectElement(document.RootElement, path);

    public static JsonElement? SelectElement(this JsonElement rootElement, string path)
    {
        var currentElement = rootElement;

        var segments = path.Trim('/').Split("/");

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

    public static JsonElement[] SelectElements(this JsonDocument document, string path) =>
        SelectElements(document.RootElement, path);

    public static JsonElement[] SelectElements(this JsonElement rootElement, string path)
    {
        var currentElement = rootElement;

        var segments = path.Trim('/').Split("/");

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

        if (currentElement.ValueKind == JsonValueKind.Array)
        {
            return currentElement.EnumerateArray().ToArray();
        }

        throw new Exception(
            $"Element found at \"{traveledPath}\" is not an array but instead \"{currentElement.ValueKind:G}\"."
        );
    }
}
