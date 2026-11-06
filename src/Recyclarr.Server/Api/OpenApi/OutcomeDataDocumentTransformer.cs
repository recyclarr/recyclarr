using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Server.Sync.Results;

namespace Recyclarr.Server.Api.OpenApi;

/// <summary>
/// Publishes the data shape of every outcome code. No endpoint references the data records (the
/// envelope types <c>data</c> as an untyped object), so the generator would not emit them. This
/// adds each record as a component schema and writes an <c>x-outcome-data</c> map from code to
/// schema on the <c>OutcomeResponse</c> schema, so generated clients get typed classes.
/// </summary>
internal sealed class OutcomeDataDocumentTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct
    )
    {
        var schemas = document.Components?.Schemas;
        if (schemas is null || !schemas.TryGetValue(nameof(OutcomeResponse), out var envelope))
        {
            return;
        }

        var map = new JsonObject();
        foreach (var code in OutcomeCodes.All)
        {
            if (code.DataType is not { } dataType)
            {
                continue;
            }

            var name = dataType.Name;
            if (!schemas.ContainsKey(name))
            {
                var schema = await context.GetOrCreateSchemaAsync(dataType, null, ct);
                document.AddComponent(name, ReferenceNamedSchemas(document, schema));
            }

            map[code.Value] = new JsonObject { ["$ref"] = $"#/components/schemas/{name}" };
        }

        if (envelope is not OpenApiSchema target)
        {
            return;
        }

        target.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        target.Extensions["x-outcome-data"] = new JsonNodeExtension(map);
    }

    // A schema created outside the document pass inlines nested types instead of referencing their
    // components, which would give generated clients duplicate classes. Each nested schema keeps
    // its component ID in the "x-schema-id" metadata; this swaps it for a reference.
    private static OpenApiSchema ReferenceNamedSchemas(
        OpenApiDocument document,
        OpenApiSchema schema
    )
    {
        if (schema.Properties is { } properties)
        {
            foreach (var key in properties.Keys.ToList())
            {
                properties[key] = Resolve(document, properties[key]);
            }
        }

        if (schema.Items is not null)
        {
            schema.Items = Resolve(document, schema.Items);
        }

        ResolveAll(document, schema.OneOf);
        ResolveAll(document, schema.AnyOf);
        ResolveAll(document, schema.AllOf);
        return schema;
    }

    private static void ResolveAll(OpenApiDocument document, IList<IOpenApiSchema>? schemas)
    {
        for (var i = 0; i < schemas?.Count; i++)
        {
            schemas[i] = Resolve(document, schemas[i]);
        }
    }

    private static IOpenApiSchema Resolve(OpenApiDocument document, IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchema inline)
        {
            return schema;
        }

        if (
            inline.Metadata?.TryGetValue("x-schema-id", out var value) != true
            || value is not string { Length: > 0 } id
        )
        {
            return ReferenceNamedSchemas(document, inline);
        }

        if (document.Components?.Schemas?.ContainsKey(id) != true)
        {
            document.AddComponent(id, ReferenceNamedSchemas(document, inline));
        }

        return new OpenApiSchemaReference(id, document);
    }
}
