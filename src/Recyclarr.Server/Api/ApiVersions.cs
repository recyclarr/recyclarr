using System.Globalization;
using System.Text.RegularExpressions;
using FastEndpoints;

namespace Recyclarr.Server.Api;

/// <summary>
/// API versions come from where endpoints live: an endpoint in a <c>V{N}</c> folder (namespace
/// segment) belongs to version N; every other endpoint belongs to version 1. Endpoints never call
/// <c>Version()</c>. The same rule sets each endpoint's route version and registers one OpenAPI
/// document per version, so adding the first endpoint of a new version creates that version.
/// </summary>
/// <remarks>
/// Versions must be known when services are registered, before FastEndpoints runs any endpoint's
/// <c>Configure()</c>, so they cannot be read from the endpoint's own configuration.
/// </remarks>
internal static partial class ApiVersions
{
    public static IReadOnlyList<int> All { get; } =
        typeof(CompositionRoot)
            .Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false } && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(Of)
            .Distinct()
            .ToList();

    public static int Of(Type endpointType) =>
        VersionSegment().Match(endpointType.Namespace ?? "") is { Success: true } match
            ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
            : 1;

    public static string DocumentName(int version) => $"v{version}";

    [GeneratedRegex(@"\.V(\d+)(?:\.|$)")]
    private static partial Regex VersionSegment();
}
