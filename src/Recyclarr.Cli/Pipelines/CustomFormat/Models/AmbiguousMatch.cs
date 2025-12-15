namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

internal record AmbiguousMatch(
    string GuideName,
    IReadOnlyList<(string Name, int Id)> ServiceMatches
);
