using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;

public record ProcessedReleaseProfileData(
    ReleaseProfileData Profile,
    IReadOnlyCollection<string> Tags
);

public class ReleaseProfileConfigPhase(
    ILogger log,
    IReleaseProfileGuideService guide,
    IReleaseProfileFilterPipeline filters)
    : IConfigPipelinePhase<ReleaseProfilePipelineContext>
{
    public Task Execute(ReleaseProfilePipelineContext context, IServiceConfiguration config)
    {
        var releaseProfiles = ((SonarrConfiguration) config).ReleaseProfiles;
        if (!releaseProfiles.Any())
        {
            log.Debug("{Instance} has no release profiles", config.InstanceName);
            return Task.CompletedTask;
        }

        var profilesFromGuide = guide.GetReleaseProfileData();
        var filteredProfiles = new List<ProcessedReleaseProfileData>();

        var configProfiles = releaseProfiles.SelectMany(x => x.TrashIds.Select(y => (TrashId: y, Config: x)));
        foreach (var (trashId, configProfile) in configProfiles)
        {
            // For each release profile specified in our YAML config, find the matching profile in the guide.
            var selectedProfile = profilesFromGuide.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
            if (selectedProfile is null)
            {
                log.Warning("A release profile with Trash ID {TrashId} does not exist", trashId);
                continue;
            }

            log.Debug("Found Release Profile: {ProfileName} ({TrashId})", selectedProfile.Name,
                selectedProfile.TrashId);

            selectedProfile = filters.Process(selectedProfile, configProfile);
            filteredProfiles.Add(new ProcessedReleaseProfileData(selectedProfile, configProfile.Tags));
        }

        context.ConfigOutput = filteredProfiles;
        return Task.CompletedTask;
    }
}
