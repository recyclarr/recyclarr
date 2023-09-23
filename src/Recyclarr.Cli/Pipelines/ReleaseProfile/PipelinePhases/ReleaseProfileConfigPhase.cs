using Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public record ProcessedReleaseProfileData(
    ReleaseProfileData Profile,
    IReadOnlyCollection<string> Tags
);

public class ReleaseProfileConfigPhase
{
    private readonly ILogger _log;
    private readonly IReleaseProfileGuideService _guide;
    private readonly IReleaseProfileFilterPipeline _filters;

    public ReleaseProfileConfigPhase(
        ILogger log,
        IReleaseProfileGuideService guide,
        IReleaseProfileFilterPipeline filters)
    {
        _log = log;
        _guide = guide;
        _filters = filters;
    }

    public IReadOnlyList<ProcessedReleaseProfileData>? Execute(SonarrConfiguration config)
    {
        if (config.ReleaseProfiles.IsEmpty())
        {
            _log.Debug("{Instance} has no release profiles", config.InstanceName);
            return null;
        }

        var profilesFromGuide = _guide.GetReleaseProfileData();
        var filteredProfiles = new List<ProcessedReleaseProfileData>();

        var configProfiles = config.ReleaseProfiles.SelectMany(x => x.TrashIds.Select(y => (TrashId: y, Config: x)));
        foreach (var (trashId, configProfile) in configProfiles)
        {
            // For each release profile specified in our YAML config, find the matching profile in the guide.
            var selectedProfile = profilesFromGuide.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
            if (selectedProfile is null)
            {
                _log.Warning("A release profile with Trash ID {TrashId} does not exist", trashId);
                continue;
            }

            _log.Debug("Found Release Profile: {ProfileName} ({TrashId})", selectedProfile.Name,
                selectedProfile.TrashId);

            selectedProfile = _filters.Process(selectedProfile, configProfile);
            filteredProfiles.Add(new ProcessedReleaseProfileData(selectedProfile, configProfile.Tags));
        }

        return filteredProfiles;
    }
}
