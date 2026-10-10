using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Recyclarr.Server;

/// <summary>
/// Appends <see cref="DescriptionAttribute"/> text on enum members to the schema description as a
/// Markdown list keyed by each member's wire value. OpenAPI has no standard field for per-value
/// descriptions.
/// </summary>
/// <remarks>
/// <para>
/// The <c>x-enumDescriptions</c> extension would render as a value table in the wiki, but
/// Refitter's NJsonSchema reader rejects its object form and fails the client build
/// (RicoSuter/NJsonSchema#1943, ADR-026). Switch to the extension once Refitter loads it.
/// </para>
/// <para>
/// Runs after FastEndpoints' enum transformer, which fills <c>enum</c> in declaration order with
/// the serialized values; this transformer pairs those values with member names by position.
/// </para>
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

        var lines = Enum.GetNames(type)
            .Zip(wireValues)
            .Select(x =>
                (
                    WireValue: x.Second?.ToString(),
                    Text: type.GetField(x.First)
                        ?.GetCustomAttribute<DescriptionAttribute>()
                        ?.Description
                )
            )
            .Where(x => x.WireValue is not null && x.Text is not null)
            .Select(x => $"- `{x.WireValue}`: {x.Text}")
            .ToList();

        if (lines.Count > 0)
        {
            schema.Description = $"{schema.Description}\n\n{string.Join('\n', lines)}".Trim();
        }

        return Task.CompletedTask;
    }
}
