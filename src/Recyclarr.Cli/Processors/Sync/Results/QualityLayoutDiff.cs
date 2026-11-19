using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync.Results;

/// <summary>
/// Turns a profile's current and desired quality lists into display lines in desired order, each
/// with a short note when it differs. A moved entry names the nearest unmoved quality it crossed,
/// as it was before the move ("was below X" when it moved up), never an index. Only entries
/// outside the longest run that kept its relative order count as moved, so moving one quality
/// past ten others flags one line, not eleven.
/// </summary>
internal static class QualityLayoutDiff
{
    public static IReadOnlyList<QualityLine> Lines(
        IReadOnlyList<QualityProfileLayoutResponse>? current,
        IReadOnlyList<QualityProfileLayoutResponse> desired
    )
    {
        if (current is null)
        {
            return [.. desired.Select(x => Line(x, note: null, movedUp: null))];
        }

        var oldIndex = current
            .Select((x, i) => (x.Name, i))
            .ToDictionary(x => x.Name, x => x.i, StringComparer.Ordinal);
        var kept = desired.Select(x => x.Name).Where(oldIndex.ContainsKey).ToList();
        var newRank = kept.Select((x, i) => (x, i))
            .ToDictionary(x => x.x, x => x.i, StringComparer.Ordinal);
        var stable = StableNames(kept, oldIndex);

        var lines = new List<QualityLine>();
        foreach (var entry in desired)
        {
            if (!oldIndex.TryGetValue(entry.Name, out var index))
            {
                lines.Add(Line(entry, Added(current, entry), movedUp: null));
                continue;
            }

            var notes = new List<string>();
            bool? movedUp = null;

            if (!stable.Contains(entry.Name))
            {
                var move = Move(entry.Name, current, oldIndex, newRank, stable);
                movedUp = move.MovedUp;
                if (move.Note is { } note)
                {
                    notes.Add(note);
                }
            }

            if (current[index].Allowed != entry.Allowed)
            {
                notes.Add(entry.Allowed ? "now allowed" : "now denied");
            }

            lines.Add(Line(entry, notes.Count == 0 ? null : string.Join("; ", notes), movedUp));
        }

        var desiredNames = desired
            .SelectMany(x => (x.Qualities ?? []).Append(x.Name))
            .ToHashSet(StringComparer.Ordinal);

        lines.AddRange(
            current
                .Where(x => !desiredNames.Contains(x.Name))
                .Select(x => Line(x, "removed", movedUp: null))
        );

        return lines;
    }

    private static QualityLine Line(
        QualityProfileLayoutResponse entry,
        string? note,
        bool? movedUp
    ) => new(entry.Name, entry.Allowed, note, movedUp, [.. entry.Qualities ?? []]);

    private static string Added(
        IReadOnlyList<QualityProfileLayoutResponse> current,
        QualityProfileLayoutResponse entry
    )
    {
        var formerGroup = current.FirstOrDefault(x => x.Qualities?.Contains(entry.Name) == true);
        if (formerGroup is not null)
        {
            return $"left group {formerGroup.Name}";
        }

        return entry.Kind == QualityProfileLayoutKind.Group ? "new group" : "added";
    }

    // Moving up means the entry now outranks the quality it used to sit below, so that quality is
    // named; moving down mirrors it. Crossing another moved entry is that entry's move, so only
    // unmoved qualities decide the direction and the name.
    private static (bool MovedUp, string? Note) Move(
        string name,
        IReadOnlyList<QualityProfileLayoutResponse> current,
        Dictionary<string, int> oldIndex,
        Dictionary<string, int> newRank,
        HashSet<string> stable
    )
    {
        var crossedUp = stable.Count(x =>
            oldIndex[x] < oldIndex[name] && newRank[x] > newRank[name]
        );
        var crossedDown = stable.Count(x =>
            oldIndex[x] > oldIndex[name] && newRank[x] < newRank[name]
        );

        var movedUp = crossedUp >= crossedDown;
        var step = movedUp ? -1 : 1;

        for (var i = oldIndex[name] + step; i >= 0 && i < current.Count; i += step)
        {
            if (stable.Contains(current[i].Name))
            {
                var relation = movedUp ? "below" : "above";
                return (movedUp, $"was {relation} {current[i].Name}");
            }
        }

        return (movedUp, null);
    }

    // Longest increasing subsequence of old positions, taken in new order: the largest set of
    // entries whose relative order did not change. Everything else is reported as moved.
    private static HashSet<string> StableNames(List<string> kept, Dictionary<string, int> oldIndex)
    {
        var length = new int[kept.Count];
        var previous = new int[kept.Count];
        var best = -1;

        for (var i = 0; i < kept.Count; i++)
        {
            length[i] = 1;
            previous[i] = -1;
            for (var j = 0; j < i; j++)
            {
                if (oldIndex[kept[j]] < oldIndex[kept[i]] && length[j] + 1 > length[i])
                {
                    length[i] = length[j] + 1;
                    previous[i] = j;
                }
            }

            if (best < 0 || length[i] > length[best])
            {
                best = i;
            }
        }

        var stable = new HashSet<string>(StringComparer.Ordinal);
        for (var i = best; i >= 0; i = previous[i])
        {
            stable.Add(kept[i]);
        }

        return stable;
    }
}
