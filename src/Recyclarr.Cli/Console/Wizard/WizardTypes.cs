namespace Recyclarr.Cli.Console.Wizard;

// Filters profiles and CF groups to a specific content category.
// Maps to the group names in the TRaSH Guides groups.json files.
internal enum GuideCategory
{
    Standard,
    Anime,
    French,
    German,
}

// A user selection identified by trash_id with a display-friendly label
internal record WizardSelection(string TrashId, string Label);

internal enum CfGroupMode
{
    // Show default groups; selected items are opted out (skipped)
    SkipDefaults,

    // Show non-default groups; selected items are opted in (added)
    AddOptional,
}

internal static class FlagSelectorHelper
{
    // Decode a FlagSelector bitmask into a list of items using a selector
    // function that maps each selected index to the desired output.
    public static IReadOnlyList<T> DecodeFlagValue<T>(
        int? flagValue,
        int count,
        Func<int, T> selector
    )
    {
        if (flagValue is null or 0)
        {
            return [];
        }

        return Enumerable
            .Range(0, count)
            .Where(i => (flagValue.Value & (1 << i)) != 0)
            .Select(selector)
            .ToList();
    }
}
