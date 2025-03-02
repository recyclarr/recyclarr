using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

internal record QualityProfileServiceData(
    IReadOnlyList<QualityProfileDto> Profiles,
    QualityProfileDto Schema
);
