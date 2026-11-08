using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Sync.Results;

internal static class QualityProfileResponseMapper
{
    public static SonarrQualityProfilePipelineResponse ToSonarrResponse(
        QualityProfilePipelineResult result
    ) =>
        new()
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = MapOutcomes(result),
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
            Items = result
                .Resources.Select(x =>
                {
                    var sonarr =
                        x as SonarrPlannedQualityProfileResource
                        ?? throw new InvalidOperationException(
                            "Sonarr Quality Profile result has no Sonarr fields"
                        );
                    var shared = MapShared(x);
                    return new SonarrQualityProfileItemResponse
                    {
                        Action = shared.Action,
                        Identity = shared.Identity,
                        Name = shared.Name,
                        UpgradeAllowed = shared.UpgradeAllowed,
                        UpgradeUntilQuality = shared.UpgradeUntilQuality,
                        UpgradeUntilScore = shared.UpgradeUntilScore,
                        MinimumFormatScore = shared.MinimumFormatScore,
                        MinimumUpgradeFormatScore = shared.MinimumUpgradeFormatScore,
                        Qualities = shared.Qualities,
                        CustomFormatScores = shared.CustomFormatScores,
                        QualitySizes = sonarr
                            .QualitySizes.Select(ResultValueMapper.MapQualitySizeItem)
                            .ToList(),
                    };
                })
                .ToList(),
        };

    public static RadarrQualityProfilePipelineResponse ToRadarrResponse(
        QualityProfilePipelineResult result
    ) =>
        new()
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = MapOutcomes(result),
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
            Items = result
                .Resources.Select(x =>
                {
                    var radarr =
                        x as RadarrPlannedQualityProfileResource
                        ?? throw new InvalidOperationException(
                            "Radarr Quality Profile result has no Radarr fields"
                        );
                    var shared = MapShared(x);
                    return new RadarrQualityProfileItemResponse
                    {
                        Action = shared.Action,
                        Identity = shared.Identity,
                        Name = shared.Name,
                        UpgradeAllowed = shared.UpgradeAllowed,
                        UpgradeUntilQuality = shared.UpgradeUntilQuality,
                        UpgradeUntilScore = shared.UpgradeUntilScore,
                        MinimumFormatScore = shared.MinimumFormatScore,
                        MinimumUpgradeFormatScore = shared.MinimumUpgradeFormatScore,
                        Qualities = shared.Qualities,
                        CustomFormatScores = shared.CustomFormatScores,
                        Language = ResultValueMapper.MapValue(radarr.Language),
                    };
                })
                .ToList(),
        };

    private static List<OutcomeResponse> MapOutcomes(QualityProfilePipelineResult result) =>
        result.Outcomes.Select(OutcomeResponseMapper.Map).ToList();

    private static QualityProfileItemResponse MapShared(PlannedQualityProfileResource resource) =>
        new()
        {
            Action = ResultValueMapper.MapAction(resource.Action),
            Identity = MapIdentity(resource.Identity),
            Name = ResultValueMapper.MapValue(resource.Name),
            UpgradeAllowed = ResultValueMapper.MapValue(resource.UpgradeAllowed),
            UpgradeUntilQuality = ResultValueMapper.MapValue(resource.UpgradeUntilQuality),
            UpgradeUntilScore = ResultValueMapper.MapValue(resource.UpgradeUntilScore),
            MinimumFormatScore = ResultValueMapper.MapValue(resource.MinimumFormatScore),
            MinimumUpgradeFormatScore = ResultValueMapper.MapValue(
                resource.MinimumUpgradeFormatScore
            ),
            Qualities = ResultValueMapper.MapValue<
                IReadOnlyList<QualityProfileQualityLayoutItem>,
                IReadOnlyList<QualityProfileLayoutResponse>
            >(resource.Qualities, x => x.Select(MapLayout).ToList()),
            CustomFormatScores = resource.CustomFormatScores.Select(MapScore).ToList(),
        };

    private static QualityProfileIdentityResponse MapIdentity(QualityProfileIdentity identity) =>
        identity switch
        {
            GuideBackedQualityProfileIdentity x => new QualityProfileIdentityResponse
            {
                Kind = QualityProfileIdentityKind.GuideBacked,
                Name = x.MappingKey.Name,
                TrashId = x.MappingKey.TrashId,
            },
            UserDefinedQualityProfileIdentity x => new QualityProfileIdentityResponse
            {
                Kind = QualityProfileIdentityKind.UserDefined,
                Name = x.Name,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, null),
        };

    private static QualityProfileLayoutResponse MapLayout(QualityProfileQualityLayoutItem item) =>
        item switch
        {
            QualityProfileQuality x => new QualityProfileLayoutResponse
            {
                Kind = QualityProfileLayoutKind.Quality,
                Name = x.Name,
                Allowed = x.Allowed,
            },
            QualityProfileQualityGroup x => new QualityProfileLayoutResponse
            {
                Kind = QualityProfileLayoutKind.Group,
                Name = x.Name,
                Allowed = x.Allowed,
                Qualities = x.Qualities,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null),
        };

    private static QualityProfileScoreResponse MapScore(QualityProfileScoreResult score) =>
        new()
        {
            Name = score.Name,
            TrashId = score.TrashId,
            Reason = score.Reason switch
            {
                Pipelines.QualityProfile.QualityProfileScoreReason.Set => Features
                    .Sync
                    .GetResults
                    .QualityProfileScoreReason
                    .Set,
                Pipelines.QualityProfile.QualityProfileScoreReason.Reset => Features
                    .Sync
                    .GetResults
                    .QualityProfileScoreReason
                    .Reset,
                Pipelines.QualityProfile.QualityProfileScoreReason.Unmanaged => Features
                    .Sync
                    .GetResults
                    .QualityProfileScoreReason
                    .Unmanaged,
                _ => throw new ArgumentOutOfRangeException(nameof(score), score.Reason, null),
            },
            Score = ResultValueMapper.MapNullableValue(score.Score),
        };
}
