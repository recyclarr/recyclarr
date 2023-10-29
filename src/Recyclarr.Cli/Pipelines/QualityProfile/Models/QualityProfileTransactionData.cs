using System.Diagnostics.CodeAnalysis;
using FluentValidation.Results;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

public record InvalidProfileData(UpdatedQualityProfile Profile, IReadOnlyCollection<ValidationFailure> Errors);

[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
public record QualityProfileTransactionData
{
    public ICollection<string> NonExistentProfiles { get; init; } = new List<string>();
    public ICollection<InvalidProfileData> InvalidProfiles { get; init; } = new List<InvalidProfileData>();
    public ICollection<ProfileWithStats> UnchangedProfiles { get; set; } = new List<ProfileWithStats>();
    public ICollection<ProfileWithStats> ChangedProfiles { get; set; } = new List<ProfileWithStats>();
}
