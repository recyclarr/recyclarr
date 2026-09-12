using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Results;

internal sealed class SyncResultsDocumentTransformer : IOpenApiDocumentTransformer
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

        if (!schemas.TryGetValue(nameof(SyncInstanceResultsResponse), out var instanceSchema))
        {
            return Task.CompletedTask;
        }

        var mappings = instanceSchema.Discriminator?.Mapping;
        if (mappings is null)
        {
            return Task.CompletedTask;
        }

        foreach (var derivedSchema in mappings.Values)
        {
            var schemaName = derivedSchema.Reference.Id;
            if (schemaName is not null && schemas.TryGetValue(schemaName, out var schema))
            {
                schema.Properties?.Remove("service");
            }
        }

        return Task.CompletedTask;
    }
}
