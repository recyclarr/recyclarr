using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Pipelines.QualityProfile;

/// <summary>
/// Sonarr v5 per-profile quality sizes. The planner resolves which size source applies to each
/// profile (PDR-008); this component turns those planned sizes into service values and writes them
/// onto the qualities the profile contains. Qualities without planned sizes keep their current
/// values.
/// </summary>
internal class SonarrV5QualityProfileFields(IQualityItemLimitFactory limitFactory)
    : IQualityProfileServiceFields
{
    private QualityItemLimits? _limits;

    public async Task LoadAsync(CancellationToken ct)
    {
        _limits = await limitFactory.Create(SupportedServices.Sonarr, ct);
    }

    public QualityProfileData ApplyDesired(
        QualityProfileData profile,
        PlannedQualityProfile planned
    )
    {
        var sonarrProfile = AsSonarrV5(profile);
        if (planned.QualitySizes is not { } plannedSizes)
        {
            return sonarrProfile;
        }

        // non-null: LoadAsync runs before any profile is processed
        var limits = _limits!;
        var sizes = new Dictionary<string, QualityItemSize>(
            sonarrProfile.QualitySizes,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var quality in plannedSizes.Qualities.Where(x => HasQuality(profile, x.Quality)))
        {
            var resolved = QualitySizeCalculator.ResolveValues(
                quality,
                plannedSizes.PreferredRatio,
                limits
            );
            sizes[quality.Quality] = QualitySizeCalculator.ToServiceSize(resolved, limits);
        }

        return sonarrProfile with
        {
            QualitySizes = sizes,
        };
    }

    // Qualities without planned sizes are copied from the current profile, so only the desired
    // sizes can differ.
    public bool HasChanges(QualityProfileData current, QualityProfileData desired)
    {
        var currentSizes = AsSonarrV5(current).QualitySizes;
        return AsSonarrV5(desired)
            .QualitySizes.Select(x =>
                Compare(x.Key, currentSizes.GetValueOrDefault(x.Key), x.Value)
            )
            .Any(x => x.Action == PlannedAction.Update);
    }

    public PlannedQualityProfileResource Describe(
        PlannedQualityProfileResource shared,
        QualityProfileData? current,
        QualityProfileData desired,
        PlannedQualityProfile planned
    )
    {
        var desiredSizes = AsSonarrV5(desired).QualitySizes;
        var currentSizes = current is null ? null : AsSonarrV5(current).QualitySizes;
        var sizes = (planned.QualitySizes?.Qualities ?? [])
            .Where(x => HasQuality(desired, x.Quality) && desiredSizes.ContainsKey(x.Quality))
            .Select(x =>
            {
                var size = desiredSizes[x.Quality];
                return currentSizes is null
                    ? PlannedQualitySizeResource.Created(
                        x.Quality,
                        new QualitySizeValue.Numeric(size.Min),
                        ToSemanticValue(size.Preferred),
                        ToSemanticValue(size.Max)
                    )
                    : Compare(x.Quality, currentSizes.GetValueOrDefault(x.Quality), size);
            })
            .ToList();

        return new SonarrPlannedQualityProfileResource(shared, sizes);
    }

    private static PlannedQualitySizeResource Compare(
        string quality,
        QualityItemSize? current,
        QualityItemSize desired
    ) =>
        PlannedQualitySizeResource.Compared(
            quality,
            (ToMinimumValue(current), new QualitySizeValue.Numeric(desired.Min)),
            (ToSemanticValue(current?.Preferred), ToSemanticValue(desired.Preferred)),
            (ToSemanticValue(current?.Max), ToSemanticValue(desired.Max))
        );

    // A quality the service reported no sizes for has an effective minimum of zero.
    private static QualitySizeValue.Numeric ToMinimumValue(QualityItemSize? size) =>
        new(size?.Min ?? 0);

    private static QualitySizeValue ToSemanticValue(decimal? value) =>
        value is { } v ? new QualitySizeValue.Numeric(v) : new QualitySizeValue.Unlimited();

    private static bool HasQuality(QualityProfileData profile, string quality) =>
        profile
            .Items.Flatten(x => x.Items)
            .Any(x => x.Quality?.Name?.Equals(quality, StringComparison.OrdinalIgnoreCase) == true);

    private static SonarrV5QualityProfileData AsSonarrV5(QualityProfileData profile)
    {
        return profile as SonarrV5QualityProfileData
            ?? throw new InvalidOperationException(
                $"Expected a Sonarr v5 quality profile but got {profile.GetType().Name}"
            );
    }
}
