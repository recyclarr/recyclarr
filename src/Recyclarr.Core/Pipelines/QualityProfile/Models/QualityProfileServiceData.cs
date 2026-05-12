using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Pipelines.QualityProfile.Models;

internal record QualityProfileServiceData(
    IReadOnlyList<QualityProfileData> Profiles,
    QualityProfileData Schema,
    IReadOnlyList<ProfileLanguage> Languages
);
