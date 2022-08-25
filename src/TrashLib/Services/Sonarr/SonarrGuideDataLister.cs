using System.Text;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Serilog;
using TrashLib.Services.Sonarr.ReleaseProfile;
using TrashLib.Services.Sonarr.ReleaseProfile.Guide;

namespace TrashLib.Services.Sonarr;

[UsedImplicitly]
public class SonarrGuideDataLister : ISonarrGuideDataLister
{
    private readonly IConsole _console;
    private readonly ISonarrGuideService _guide;
    private readonly ILogger _log;

    public SonarrGuideDataLister(IConsole console, ISonarrGuideService guide, ILogger log)
    {
        _console = console;
        _guide = guide;
        _log = log;
    }

    public void ListReleaseProfiles()
    {
        _console.Output.WriteLine("\nList of Release Profiles in the TRaSH Guides:\n");

        var profilesFromGuide = _guide.GetReleaseProfileData();
        foreach (var profile in profilesFromGuide)
        {
            _console.Output.WriteLine($"          - {profile.TrashId} # {profile.Name}");
        }

        _console.Output.WriteLine(
            "\nThe above Release Profiles are in YAML format and ready to be copied & pasted under the `trash_ids:` property.");
    }

    public void ListTerms(string releaseProfileId)
    {
        _console.Output.WriteLine();

        var profile = _guide.GetUnfilteredProfileById(releaseProfileId);
        if (profile is null)
        {
            _log.Error("No release profile found with that Trash ID");
            return;
        }

        var validator = new ReleaseProfileDataValidator();
        if (!validator.Validate(profile).IsValid)
        {
            _console.Output.WriteLine("This release profile has no terms that can be filtered. " +
                                      "Terms must have Trash IDs assigned in order to be filtered.");
            return;
        }

        _console.Output.WriteLine($"List of Terms for the '{profile.Name}' Release Profile that may be filtered:\n");

        PrintTerms(profile.Required, "Required");
        PrintTerms(profile.Ignored, "Ignored");
        PrintTerms(profile.Preferred.SelectMany(x => x.Terms), "Preferred");

        _console.Output.WriteLine(
            "The above Term Filters are in YAML format and ready to be copied & pasted under the `include:` or `exclude:` filter properties.");
    }

    private void PrintTerms(IEnumerable<TermData> terms, string category)
    {
        var filteredTerms = terms.Where(x => x.TrashId.Any()).ToList();
        if (!filteredTerms.Any())
        {
            return;
        }

        _console.Output.WriteLine($"{category} Terms:\n");
        foreach (var term in filteredTerms)
        {
            var line = new StringBuilder($"            - {term.TrashId}");
            if (term.Name.Any())
            {
                line.Append($" # {term.Name}");
            }

            _console.Output.WriteLine(line);
        }

        _console.Output.WriteLine();
    }
}
