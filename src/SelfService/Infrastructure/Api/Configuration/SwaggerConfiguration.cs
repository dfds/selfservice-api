using Microsoft.OpenApi.Models;

namespace SelfService.Infrastructure.Api.Configuration;

public static class SwaggerConfiguration
{
    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Description = "SelfService API",
                    Version = "v1",
                    Title = "SelfService API",
                }
            );
            var schemaHelper = new SwashbuckleSchemaHelper();
            options.CustomSchemaIds(type => schemaHelper.GetSchemaId(type));

            options.DocInclusionPredicate((_, description) => !description.ShouldIgnore());
        });
    }
}

public class SwashbuckleSchemaHelper
{
    private readonly Dictionary<string, List<string>> _schemaNameRepetition = new();

    private string DefaultSchemaIdSelector(Type modelType)
    {
        if (!modelType.IsConstructedGenericType)
            return modelType.Name.Replace("[]", "Array");

        var prefix = modelType
            .GetGenericArguments()
            .Select(genericArg => DefaultSchemaIdSelector(genericArg))
            .Aggregate((previous, current) => previous + current);

        return prefix + modelType.Name.Split('`').First();
    }

    public string GetSchemaId(Type modelType)
    {
        string id = DefaultSchemaIdSelector(modelType);

        if (!_schemaNameRepetition.ContainsKey(id))
            _schemaNameRepetition.Add(id, new List<string>());

        var modelNameList = _schemaNameRepetition[id];
        var fullName = modelType.FullName ?? "";
        if (!string.IsNullOrEmpty(fullName) && !modelNameList.Contains(fullName))
            modelNameList.Add(fullName);

        int index = modelNameList.IndexOf(fullName);

        return $"{id}{(index >= 1 ? index.ToString() : "")}";
    }
}
