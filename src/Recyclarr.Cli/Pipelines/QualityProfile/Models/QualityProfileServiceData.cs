using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

internal record QualityProfileServiceData(
    IReadOnlyList<QualityProfileData> Profiles,
    QualityProfileData Schema,
    IReadOnlyList<ProfileLanguage> Languages
);
