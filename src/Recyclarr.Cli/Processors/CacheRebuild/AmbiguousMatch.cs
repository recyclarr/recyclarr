namespace Recyclarr.Cli.Processors.CacheRebuild;

internal record AmbiguousMatch(
    string GuideName,
    IReadOnlyList<(string Name, int Id)> ServiceMatches
);
