using System.Text;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashGuide.ReleaseProfile;
using Spectre.Console;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile;

public class ReleaseProfileDataLister(IAnsiConsole console, IReleaseProfileGuideService guide)
{
    public void ListReleaseProfiles()
    {
        console.WriteLine("\nList of Release Profiles in the TRaSH Guides:\n");

        var profilesFromGuide = guide.GetReleaseProfileData();
        foreach (var profile in profilesFromGuide)
        {
            console.WriteLine($"          - {profile.TrashId} # {profile.Name}");
        }

        console.WriteLine(
            "\nThe above Release Profiles are in YAML format and ready to be copied & pasted under the `trash_ids:` property.");
    }

    private static bool HasIdentifiableTerms(ReleaseProfileData profile)
    {
        static bool HasTrashIds(IEnumerable<TermData> terms)
        {
            return terms.Any(x => !string.IsNullOrEmpty(x.TrashId));
        }

        return
            HasTrashIds(profile.Ignored) ||
            HasTrashIds(profile.Required) ||
            HasTrashIds(profile.Preferred.SelectMany(x => x.Terms));
    }

    public void ListTerms(string releaseProfileId)
    {
        var profile = guide.GetReleaseProfileData()
            .FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(releaseProfileId));

        if (profile is null)
        {
            throw new ArgumentException("No release profile found with that Trash ID");
        }

        if (!HasIdentifiableTerms(profile))
        {
            throw new ArgumentException(
                "This release profile has no terms that can be filtered " +
                "(terms must have Trash IDs assigned in order to be filtered)");
        }

        console.WriteLine();
        console.WriteLine($"List of Terms for the '{profile.Name}' Release Profile that may be filtered:\n");

        PrintTerms(profile.Required, "Required");
        PrintTerms(profile.Ignored, "Ignored");
        PrintTerms(profile.Preferred.SelectMany(x => x.Terms), "Preferred");

        console.WriteLine(
            "The above Term Filters are in YAML format and ready to be copied & pasted under the `include:` or `exclude:` filter properties.");
    }

    private void PrintTerms(IEnumerable<TermData> terms, string category)
    {
        var filteredTerms = terms.Where(x => x.TrashId.Length != 0).ToList();
        if (filteredTerms.Count == 0)
        {
            return;
        }

        console.WriteLine($"{category} Terms:\n");
        foreach (var term in filteredTerms)
        {
            var line = new StringBuilder($"            - {term.TrashId}");
            if (term.Name.Length != 0)
            {
                line.Append($" # {term.Name}");
            }

            console.WriteLine(line.ToString());
        }

        console.WriteLine();
    }
}
