using CliFx.Infrastructure;
using JetBrains.Annotations;
using TrashLib.Sonarr.ReleaseProfile.Guide;

namespace TrashLib.Sonarr;

[UsedImplicitly]
public class ReleaseProfileLister : IReleaseProfileLister
{
    private readonly IConsole _console;
    private readonly ISonarrGuideService _guide;

    public ReleaseProfileLister(IConsole console, ISonarrGuideService guide)
    {
        _console = console;
        _guide = guide;
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
}
