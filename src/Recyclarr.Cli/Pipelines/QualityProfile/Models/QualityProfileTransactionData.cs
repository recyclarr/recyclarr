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

    // Error cases
    public Collection<InvalidProfileData> InvalidProfiles { get; } = [];
    public Collection<ConflictingQualityProfile> ConflictingProfiles { get; } = [];
    public Collection<AmbiguousQualityProfile> AmbiguousProfiles { get; } = [];
}
