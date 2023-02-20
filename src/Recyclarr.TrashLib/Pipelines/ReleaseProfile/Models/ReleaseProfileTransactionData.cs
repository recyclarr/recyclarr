using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Api.Objects;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Models;

public record ReleaseProfileTransactionData(
    IReadOnlyCollection<SonarrReleaseProfile> UpdatedProfiles,
    IReadOnlyCollection<SonarrReleaseProfile> CreatedProfiles,
    IReadOnlyCollection<SonarrReleaseProfile> DeletedProfiles
);
