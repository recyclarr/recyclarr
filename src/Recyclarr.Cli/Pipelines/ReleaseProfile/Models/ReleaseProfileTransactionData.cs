using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Models;

public record ReleaseProfileTransactionData(
    IReadOnlyCollection<SonarrReleaseProfile> UpdatedProfiles,
    IReadOnlyCollection<SonarrReleaseProfile> CreatedProfiles,
    IReadOnlyCollection<SonarrReleaseProfile> DeletedProfiles
);
