using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Recyclarr.Server.Api.OpenApi;

/// <summary>
/// Publishes <see cref="DescriptionAttribute"/> text on enum members as an
/// <c>x-enumDescriptions</c> object keyed by each member's wire value, which the wiki API reference
/// renders as a value table. OpenAPI has no standard field for per-value descriptions.
/// </summary>
/// <remarks>
/// Runs after FastEndpoints' enum transformer, which fills <c>enum</c> in declaration order with
/// the serialized values; this transformer pairs those values with member names by position.
/// </remarks>
internal sealed class EnumDescriptionSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken ct
    )
    {
        var type = context.JsonTypeInfo.Type;
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (!type.IsEnum || schema.Enum is not { Count: > 0 } wireValues)
        {
            return Task.CompletedTask;
        }

        var descriptions = new JsonObject();
        foreach (var (name, wireValue) in Enum.GetNames(type).Zip(wireValues))
        {
            var text = type.GetField(name)?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (wireValue?.GetValue<string>() is { } key && text is not null)
            {
                descriptions[key] = text;
            }
        }

        if (descriptions.Count > 0)
        {
            schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            schema.Extensions["x-enumDescriptions"] = new JsonNodeExtension(descriptions);
        }

        return Task.CompletedTask;
    }
}
