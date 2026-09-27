using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Recyclarr.Server.Features.Guide.Naming;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server;

// Polymorphic response types declare their discriminator on the base schema. Removing it from each
// derived schema keeps generated clients from seeing the property twice.
internal sealed class DiscriminatorDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct
    )
    {
        var schemas = document.Components?.Schemas;
        if (schemas is null)
        {
            return Task.CompletedTask;
        }

        RemoveDiscriminatorProperties(schemas, nameof(SyncInstanceResultsResponse), "service");
        RemoveDiscriminatorProperties(schemas, nameof(PlanningOutcomeResponse), "type");
        RemoveDiscriminatorProperties(schemas, nameof(GuideNamingResponse), "service");
        return Task.CompletedTask;
    }

    private static void RemoveDiscriminatorProperties(
        IDictionary<string, IOpenApiSchema> schemas,
        string baseSchemaName,
        string discriminatorProperty
    )
    {
        if (!schemas.TryGetValue(baseSchemaName, out var baseSchema))
        {
            return;
        }

        var mappings = baseSchema.Discriminator?.Mapping;
        if (mappings is null)
        {
            return;
        }

        foreach (var derivedSchema in mappings.Values)
        {
            var schemaName = derivedSchema.Reference.Id;
            if (schemaName is not null && schemas.TryGetValue(schemaName, out var schema))
            {
                schema.Properties?.Remove(discriminatorProperty);
            }
        }
    }
}
