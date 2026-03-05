using System.Collections.ObjectModel;
using FluentValidation.Results;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

internal record InvalidProfileData(
    UpdatedQualityProfile Profile,
    IReadOnlyCollection<ValidationFailure> Errors
);

internal record QualityProfileTransactionData
{
    // Success cases - collection membership indicates the "reason"
    public Collection<UpdatedQualityProfile> NewProfiles { get; } = [];
    public Collection<ProfileWithStats> UpdatedProfiles { get; } = [];
    public Collection<UpdatedQualityProfile> UnchangedProfiles { get; } = [];

    // Warning/info cases
    public Collection<string> NonExistentProfiles { get; } = [];

    // Profiles that already existed in the service and were replaced (for diagnostic warnings)
    public Collection<string> ReplacedProfiles { get; } = [];

    // Error cases
    public Collection<InvalidProfileData> InvalidProfiles { get; } = [];
    public Collection<string> RenameConflicts { get; } = [];
    public Collection<AmbiguousQualityProfile> AmbiguousProfiles { get; } = [];
}
