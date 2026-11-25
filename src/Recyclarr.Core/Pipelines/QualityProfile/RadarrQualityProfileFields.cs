using FluentValidation.Results;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.QualityProfile;

/// <summary>
/// Radarr's profile language. A configured <c>language</c> wins and must name a language the
/// instance offers, or the profile is rejected. Without one, the guide's language applies when the
/// instance offers it; otherwise the profile keeps its current language.
/// </summary>
internal class RadarrQualityProfileFields(IRadarrLanguageService languageService)
    : IQualityProfileServiceFields
{
    private IReadOnlyList<ProfileLanguage> _languages = [];

    public async Task LoadAsync(CancellationToken ct)
    {
        _languages = await languageService.GetLanguages(ct);
    }

    // A configured language that the instance lacks leaves the language unchanged; Validate
    // rejects the profile, so it never falls back to the guide or current value.
    public QualityProfileData ApplyDesired(
        QualityProfileData profile,
        PlannedQualityProfile planned
    )
    {
        var radarrProfile = AsRadarr(profile);
        var language = planned.Config.Language is { } configured
            ? FindLanguage(configured)
            : FindGuideLanguage(planned);
        return language is null ? radarrProfile : radarrProfile with { Language = language };
    }

    public IReadOnlyCollection<ValidationFailure> Validate(UpdatedQualityProfile profile)
    {
        if (profile.ProfileConfig.Config.Language is not { } configured)
        {
            return [];
        }

        if (FindLanguage(configured) is not null)
        {
            return [];
        }

        return
        [
            new ValidationFailure(
                "language",
                "`language` names a language the service does not offer"
            )
            {
                ErrorCode = nameof(QualityProfileValidationConstraint.LanguageReferenceMismatch),
            },
        ];
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
            QualityProfileField.Of(
                planned.Config.Language is not null || FindGuideLanguage(planned) is not null,
                current is null
                    ? PlannedValue.Created(LanguageOf(desired))
                    : PlannedValue.Compared(LanguageOf(current), LanguageOf(desired))
            )
        );

    private ProfileLanguage? FindGuideLanguage(PlannedQualityProfile planned)
    {
        var guideLanguage = planned.GuideResource is RadarrQualityProfileResource guide
            ? guide.Language
            : "";

        return string.IsNullOrEmpty(guideLanguage) ? null : FindLanguage(guideLanguage);
    }

    private ProfileLanguage? FindLanguage(string name) =>
        _languages.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

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
