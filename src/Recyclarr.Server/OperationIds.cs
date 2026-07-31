using FastEndpoints;

namespace Recyclarr.Server;

internal static class OperationIds
{
    private const string FeaturesMarker = ".Features.";

    // Builds an operation id from an endpoint's feature-slice location rather than its type name.
    // Every endpoint class is named "Endpoint", so the type name alone carries no information;
    // the namespace segments after "Features" identify it (e.g. Sync.GetJob -> SyncGetJob).
    public static string Generate(EndpointNameGenerationContext ctx)
    {
        var fullName = ctx.EndpointType.FullName ?? ctx.EndpointType.Name;
        var markerIndex = fullName.IndexOf(FeaturesMarker, StringComparison.Ordinal);

        if (markerIndex < 0)
        {
            return ctx.EndpointType.Name;
        }

        var featurePath = fullName[(markerIndex + FeaturesMarker.Length)..];

        // Trailing segment is always the "Endpoint" class itself; the rest is the feature path.
        var segments = featurePath.Split('.')[..^1];
        return string.Concat(segments);
    }
}
