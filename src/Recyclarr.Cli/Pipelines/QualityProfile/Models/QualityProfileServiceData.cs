using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

public record QualityProfileServiceData(
    IReadOnlyList<QualityProfileDto> Profiles,
    QualityProfileDto Schema
);
