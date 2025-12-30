namespace Recyclarr.Cache;

public record AmbiguousMatch(string GuideName, IReadOnlyList<(string Name, int Id)> ServiceMatches);
