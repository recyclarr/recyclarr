using System.Collections.ObjectModel;
using FluentValidation.Results;
using Recyclarr.Pipelines.Plan;

namespace Recyclarr.Pipelines.QualityProfile.Models;

internal record InvalidProfileData(
    UpdatedQualityProfile Profile,
    IReadOnlyCollection<ValidationFailure> Errors
);

internal record ReplacedProfileData(PlannedQualityProfile Profile, int ServiceId);

internal record RenameConflictData(
    PlannedQualityProfile Profile,
    string ConflictName,
    int ConflictId
);

public record QualityProfileTransactionData
{
    // Success cases - collection membership indicates the "reason"
    public Collection<UpdatedQualityProfile> NewProfiles { get; } = [];
    public Collection<ProfileWithStats> UpdatedProfiles { get; } = [];
    public Collection<UpdatedQualityProfile> UnchangedProfiles { get; } = [];

    // Warning/info cases
    internal Collection<PlannedQualityProfile> NonExistentProfiles { get; } = [];

    // Profiles that already existed in the service and were replaced (for diagnostic warnings)
    internal Collection<ReplacedProfileData> ReplacedProfiles { get; } = [];

    // Error cases
    internal Collection<InvalidProfileData> InvalidProfiles { get; } = [];
    internal Collection<RenameConflictData> RenameConflicts { get; } = [];
    internal Collection<AmbiguousQualityProfile> AmbiguousProfiles { get; } = [];
}
