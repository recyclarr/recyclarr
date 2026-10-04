using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Sync.Results;
using Riok.Mapperly.Abstractions;

namespace Recyclarr.Server.Sync.Results;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class QualityProfileResponseMapper
{
    public static SonarrQualityProfilePipelineResponse ToSonarrResponse(
        QualityProfilePipelineResult result
    )
    {
        var creates = result
            .Deltas.OfType<QualityProfileCreateDelta>()
            .Select(x =>
                x.State.GetType() == typeof(QualityProfileControlledState)
                    ? new QualityProfileCreateResponse(
                        MapIdentity(x.Identity),
                        MapSharedState(x.State)
                    )
                    : throw new InvalidOperationException(
                        $"Sonarr Quality Profile create has {x.State.GetType().Name}"
                    )
            )
            .ToList();
        var updates = result
            .Deltas.OfType<QualityProfileUpdateDelta>()
            .Select(x =>
            {
                var response = MapSharedUpdate(x);
                EnsureUpdateMapped(x, response, serviceComponents: 0);
                return response;
            })
            .ToList();
        EnsureDeltasMapped(result, creates.Count, updates.Count);

        return new SonarrQualityProfilePipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = MapOutcomes(result),
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
            Creates = creates,
            Updates = updates,
        };
    }

    public static RadarrQualityProfilePipelineResponse ToRadarrResponse(
        QualityProfilePipelineResult result
    )
    {
        var creates = result
            .Deltas.OfType<QualityProfileCreateDelta>()
            .Select(x =>
            {
                var state =
                    x.State as RadarrQualityProfileControlledState
                    ?? throw new InvalidOperationException(
                        "Radarr Quality Profile create has no Radarr state"
                    );
                var shared = MapSharedState(state);
                return new RadarrQualityProfileCreateResponse(
                    MapIdentity(x.Identity),
                    new RadarrQualityProfileControlledStateResponse
                    {
                        Name = shared.Name,
                        UpgradeAllowed = shared.UpgradeAllowed,
                        UpgradeUntilQuality = shared.UpgradeUntilQuality,
                        UpgradeUntilScore = shared.UpgradeUntilScore,
                        MinimumFormatScore = shared.MinimumFormatScore,
                        MinimumUpgradeFormatScore = shared.MinimumUpgradeFormatScore,
                        Qualities = shared.Qualities,
                        CustomFormatScores = shared.CustomFormatScores,
                        Language = state.Language,
                    }
                );
            })
            .ToList();
        var updates = result
            .Deltas.OfType<QualityProfileUpdateDelta>()
            .Select(x =>
            {
                var shared = MapSharedUpdate(x);
                var response = new RadarrQualityProfileUpdateResponse
                {
                    Identity = shared.Identity,
                    Name = shared.Name,
                    UpgradeAllowed = shared.UpgradeAllowed,
                    UpgradeUntilQuality = shared.UpgradeUntilQuality,
                    UpgradeUntilScore = shared.UpgradeUntilScore,
                    MinimumFormatScore = shared.MinimumFormatScore,
                    MinimumUpgradeFormatScore = shared.MinimumUpgradeFormatScore,
                    QualityLayout = shared.QualityLayout,
                    CustomFormatScores = shared.CustomFormatScores,
                    Language = GetValue<RadarrQualityProfileLanguageChanged, string?>(
                        x,
                        y => y.Value
                    ),
                };
                EnsureUpdateMapped(x, response, response.Language is null ? 0 : 1);
                return response;
            })
            .ToList();
        EnsureDeltasMapped(result, creates.Count, updates.Count);

        return new RadarrQualityProfilePipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = MapOutcomes(result),
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
            Creates = creates,
            Updates = updates,
        };
    }

    private static QualityProfileOutcomesResponse MapOutcomes(QualityProfilePipelineResult result)
    {
        var outcomes = MapOutcomes(result.Outcomes);
        ResultValueMapper.EnsureAllMapped(
            "Quality Profile outcome",
            result.Outcomes.Count,
            outcomes.ReferenceMismatches.Count,
            outcomes.DuplicateNames.Count,
            outcomes.ScoreCollisions.Count,
            outcomes.NotFound.Count,
            outcomes.Adopted.Count,
            outcomes.MinimumScoresUnsatisfied.Count,
            outcomes.InvalidCutoffs.Count,
            outcomes.UnavailableCutoffs.Count,
            outcomes.QualitiesRequired.Count,
            outcomes.QualityReferenceMismatches.Count,
            outcomes.ResetScoreReferenceMismatches.Count,
            outcomes.RenameBlocked.Count,
            outcomes.AmbiguousMatches.Count,
            outcomes.CreateRejected.Count,
            outcomes.UpdateRejected.Count
        );
        return outcomes;
    }

    private static void EnsureDeltasMapped(
        QualityProfilePipelineResult result,
        int creates,
        int updates
    ) =>
        ResultValueMapper.EnsureAllMapped(
            "Quality Profile delta",
            result.Deltas.Count,
            creates,
            updates
        );

    private static QualityProfileControlledStateResponse MapSharedState(
        QualityProfileControlledState state
    ) =>
        new()
        {
            Name = state.Name,
            UpgradeAllowed = state.UpgradeAllowed,
            UpgradeUntilQuality = state.UpgradeUntilQuality,
            UpgradeUntilScore = state.UpgradeUntilScore,
            MinimumFormatScore = state.MinimumFormatScore,
            MinimumUpgradeFormatScore = state.MinimumUpgradeFormatScore,
            Qualities = state.Qualities.Select(MapLayout).ToList(),
            CustomFormatScores = state.CustomFormatScores.Select(MapScore).ToList(),
        };

    private static QualityProfileUpdateResponse MapSharedUpdate(QualityProfileUpdateDelta delta)
    {
        return new QualityProfileUpdateResponse
        {
            Identity = MapIdentity(delta.Identity),
            Name = GetValue<QualityProfileNameChanged, string>(delta, x => x.Value),
            UpgradeAllowed = GetValue<QualityProfileUpgradeAllowedChanged, bool?>(
                delta,
                x => x.Value
            ),
            UpgradeUntilQuality = GetValue<QualityProfileUpgradeUntilQualityChanged, string?>(
                delta,
                x => x.Value
            ),
            UpgradeUntilScore = GetValue<QualityProfileUpgradeUntilScoreChanged, int?>(
                delta,
                x => x.Value
            ),
            MinimumFormatScore = GetValue<QualityProfileMinimumFormatScoreChanged, int?>(
                delta,
                x => x.Value
            ),
            MinimumUpgradeFormatScore = GetValue<
                QualityProfileMinimumUpgradeFormatScoreChanged,
                int?
            >(delta, x => x.Value),
            QualityLayout = delta
                .Components.OfType<QualityProfileQualityLayoutChanged>()
                .Select(x => new ValueChangeResponse<IReadOnlyList<QualityProfileLayoutResponse>>(
                    x.Current.Select(MapLayout).ToList(),
                    x.Desired.Select(MapLayout).ToList()
                ))
                .SingleOrDefault(),
            CustomFormatScores = delta
                .Components.OfType<QualityProfileCustomFormatScoreChanged>()
                .Select(MapScoreChange)
                .ToList(),
        };
    }

    // A component of the other service's type maps to no field, so it fails this check.
    private static void EnsureUpdateMapped(
        QualityProfileUpdateDelta delta,
        QualityProfileUpdateResponse response,
        int serviceComponents
    ) =>
        ResultValueMapper.EnsureAllMapped(
            "Quality Profile update component",
            delta.Components.Count,
            response.Name is null ? 0 : 1,
            response.UpgradeAllowed is null ? 0 : 1,
            response.UpgradeUntilQuality is null ? 0 : 1,
            response.UpgradeUntilScore is null ? 0 : 1,
            response.MinimumFormatScore is null ? 0 : 1,
            response.MinimumUpgradeFormatScore is null ? 0 : 1,
            response.QualityLayout is null ? 0 : 1,
            response.CustomFormatScores.Count,
            serviceComponents
        );

    private static ValueChangeResponse<TValue>? GetValue<TComponent, TValue>(
        QualityProfileUpdateDelta delta,
        Func<TComponent, ValueDelta<TValue>> select
    )
        where TComponent : QualityProfileUpdateComponent =>
        delta
            .Components.OfType<TComponent>()
            .Select(x => ResultValueMapper.MapValue(select(x)))
            .SingleOrDefault();

    private static QualityProfileOutcomesResponse MapOutcomes(
        IReadOnlyList<QualityProfileOutcome> outcomes
    ) =>
        new()
        {
            ReferenceMismatches = outcomes
                .OfType<QualityProfileReferenceMismatchOutcome>()
                .Select(x => x.TrashId)
                .ToList(),
            DuplicateNames = outcomes
                .OfType<QualityProfileDuplicateNameOutcome>()
                .Select(x => new QualityProfileNamedOutcomeResponse(x.Name))
                .ToList(),
            ScoreCollisions = outcomes
                .OfType<QualityProfileScoreCollisionOutcome>()
                .Select(MapScoreCollision)
                .ToList(),
            NotFound = outcomes
                .OfType<QualityProfileNotFoundOutcome>()
                .Select(x => MapIdentityOutcome(x.Identity))
                .ToList(),
            Adopted = outcomes
                .OfType<QualityProfileAdoptedOutcome>()
                .Select(x => new QualityProfileAdoptedResponse(
                    MapIdentity(x.Identity),
                    x.ServiceId
                ))
                .ToList(),
            MinimumScoresUnsatisfied = outcomes
                .OfType<QualityProfileMinimumScoreUnsatisfiedOutcome>()
                .Select(x => new QualityProfileMinimumScoreResponse(
                    MapIdentity(x.Identity),
                    x.MinimumScore,
                    x.TotalPositiveScore,
                    x.MaximumScore
                ))
                .ToList(),
            InvalidCutoffs = outcomes
                .OfType<QualityProfileInvalidCutoffOutcome>()
                .Select(x => MapQualityName(x.Identity, x.QualityName))
                .ToList(),
            UnavailableCutoffs = outcomes
                .OfType<QualityProfileUnavailableCutoffOutcome>()
                .Select(x => MapQualityName(x.Identity, x.QualityName))
                .ToList(),
            QualitiesRequired = outcomes
                .OfType<QualityProfileQualitiesRequiredOutcome>()
                .Select(x => MapIdentityOutcome(x.Identity))
                .ToList(),
            QualityReferenceMismatches = outcomes
                .OfType<QualityProfileQualityReferenceMismatchOutcome>()
                .Select(x => new QualityProfileNamesResponse(MapIdentity(x.Identity), x.Names))
                .ToList(),
            ResetScoreReferenceMismatches = outcomes
                .OfType<QualityProfileResetScoreReferenceMismatchOutcome>()
                .Select(x => new QualityProfileResetReferencesResponse(
                    MapIdentity(x.Identity),
                    x.Names,
                    x.Patterns
                ))
                .ToList(),
            RenameBlocked = outcomes
                .OfType<QualityProfileRenameBlockedOutcome>()
                .Select(x => new QualityProfileRenameBlockedResponse(
                    MapIdentity(x.Identity),
                    MapServiceMatch(x.Conflict)
                ))
                .ToList(),
            AmbiguousMatches = outcomes
                .OfType<QualityProfileAmbiguousMatchOutcome>()
                .Select(x => new QualityProfileAmbiguousMatchResponse(
                    MapIdentity(x.Identity),
                    x.ServiceMatches.Select(MapServiceMatch).ToList()
                ))
                .ToList(),
            CreateRejected = outcomes
                .OfType<QualityProfileCreateRejectedOutcome>()
                .Select(x => MapIdentityOutcome(x.Identity))
                .ToList(),
            UpdateRejected = outcomes
                .OfType<QualityProfileUpdateRejectedOutcome>()
                .Select(x => MapIdentityOutcome(x.Identity))
                .ToList(),
        };

    private static QualityProfileIdentityResponse MapIdentity(QualityProfileIdentity identity) =>
        identity switch
        {
            GuideBackedQualityProfileIdentity x => new QualityProfileIdentityResponse(
                QualityProfileIdentityKind.GuideBacked,
                x.MappingKey.Name
            )
            {
                TrashId = x.MappingKey.TrashId,
            },
            UserDefinedQualityProfileIdentity x => new QualityProfileIdentityResponse(
                QualityProfileIdentityKind.UserDefined,
                x.Name
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, null),
        };

    private static QualityProfileLayoutResponse MapLayout(QualityProfileQualityLayoutItem item) =>
        item switch
        {
            QualityProfileQuality x => new QualityProfileLayoutResponse(
                QualityProfileLayoutKind.Quality,
                x.Name,
                x.Allowed
            ),
            QualityProfileQualityGroup x => new QualityProfileLayoutResponse(
                QualityProfileLayoutKind.Group,
                x.Name,
                x.Allowed
            )
            {
                Qualities = x.Qualities,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null),
        };

    private static QualityProfileIdentityOutcomeResponse MapIdentityOutcome(
        QualityProfileIdentity identity
    ) => new(MapIdentity(identity));

    private static QualityProfileQualityNameResponse MapQualityName(
        QualityProfileIdentity identity,
        string qualityName
    ) => new(MapIdentity(identity), qualityName);

    private static QualityProfileScoreChangeResponse MapScoreChange(
        QualityProfileCustomFormatScoreChanged change
    ) =>
        new(
            change.Name,
            ResultValueMapper.MapValue(change.Value),
            change.Reason switch
            {
                QualityProfileScoreChangeReason.Set => QualityProfileScoreReason.Set,
                QualityProfileScoreChangeReason.Reset => QualityProfileScoreReason.Reset,
                _ => throw new ArgumentOutOfRangeException(nameof(change)),
            }
        )
        {
            TrashId = change.TrashId,
        };

    private static partial QualityProfileScoreResponse MapScore(
        QualityProfileCustomFormatScore source
    );

    private static partial NamedServiceResourceResponse MapServiceMatch(
        QualityProfileServiceMatch source
    );

    private static partial QualityProfileFormatReferenceResponse MapFormatReference(
        QualityProfileCustomFormatReference source
    );

    private static QualityProfileScoreCollisionResponse MapScoreCollision(
        QualityProfileScoreCollisionOutcome source
    ) =>
        new(
            MapFormatReference(source.Existing),
            MapFormatReference(source.Rejected),
            source.ServiceId
        );
}
