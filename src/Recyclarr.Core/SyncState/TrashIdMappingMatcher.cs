namespace Recyclarr.SyncState;

public record TrashIdMatchResult(
    IReadOnlyList<TrashIdMapping> Matches,
    IReadOnlyList<AmbiguousMatch> Ambiguous
);

public static class TrashIdMappingMatcher
{
    public static TrashIdMatchResult Match(
        IEnumerable<IGuideResource> guideResources,
        IEnumerable<IServiceResource> serviceResources
    )
    {
        List<TrashIdMapping> matches = [];
        List<AmbiguousMatch> ambiguous = [];
        var serviceByName = serviceResources.ToLookup(
            s => s.Name,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var guide in guideResources)
        {
            var nameMatches = serviceByName[guide.Name].Select(s => (s.Name, s.Id)).ToList();

            switch (nameMatches.Count)
            {
                case 1:
                    matches.Add(new TrashIdMapping(guide.TrashId, guide.Name, nameMatches[0].Id));
                    break;
                case > 1:
                    ambiguous.Add(new AmbiguousMatch(guide.Name, nameMatches));
                    break;
                // case 0: no match in service - will be created on sync
            }
        }

        return new TrashIdMatchResult(matches, ambiguous);
    }
}
