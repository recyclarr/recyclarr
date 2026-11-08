using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.QualityProfile;

/// <summary>
/// Radarr's profile language. The guide names a language; it applies only when the instance
/// offers a language with that name, and otherwise the profile keeps its current language.
/// </summary>
internal class RadarrQualityProfileFields(IRadarrLanguageService languageService)
    : IQualityProfileServiceFields
{
    private IReadOnlyList<ProfileLanguage> _languages = [];

    public async Task LoadAsync(CancellationToken ct)
    {
        _languages = await languageService.GetLanguages(ct);
    }

    public QualityProfileData ApplyDesired(
        QualityProfileData profile,
        PlannedQualityProfile planned
    )
    {
        var radarrProfile = AsRadarr(profile);
        var guideLanguage = planned.GuideResource is RadarrQualityProfileResource guide
            ? guide.Language
            : "";

        if (string.IsNullOrEmpty(guideLanguage))
        {
            return radarrProfile;
        }

        var language = _languages.FirstOrDefault(l =>
            l.Name.Equals(guideLanguage, StringComparison.OrdinalIgnoreCase)
        );

        return language is null ? radarrProfile : radarrProfile with { Language = language };
    }

    public bool HasChanges(QualityProfileData current, QualityProfileData desired) =>
        LanguageOf(current) != LanguageOf(desired);

    public PlannedQualityProfileResource Describe(
        PlannedQualityProfileResource shared,
        QualityProfileData? current,
        QualityProfileData desired,
        PlannedQualityProfile planned
    ) =>
        new RadarrPlannedQualityProfileResource(
            shared,
            current is null
                ? PlannedValue.Created(LanguageOf(desired))
                : PlannedValue.Compared(LanguageOf(current), LanguageOf(desired))
        );

    private static string? LanguageOf(QualityProfileData profile) =>
        AsRadarr(profile).Language?.Name;

    private static RadarrQualityProfileData AsRadarr(QualityProfileData profile)
    {
        return profile as RadarrQualityProfileData
            ?? throw new InvalidOperationException(
                $"Expected a Radarr quality profile but got {profile.GetType().Name}"
            );
    }
}
