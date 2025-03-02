using System.Diagnostics.CodeAnalysis;
using FluentValidation.Results;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

internal record InvalidProfileData(
    UpdatedQualityProfile Profile,
    IReadOnlyCollection<ValidationFailure> Errors
);

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
internal record QualityProfileTransactionData
{
    public ICollection<string> NonExistentProfiles { get; init; } = [];
    public ICollection<InvalidProfileData> InvalidProfiles { get; init; } = [];
    public ICollection<ProfileWithStats> UnchangedProfiles { get; set; } = [];
    public ICollection<ProfileWithStats> ChangedProfiles { get; set; } = [];
}
