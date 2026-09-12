using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Tests.Http;

internal static class SyncResultScenarios
{
    public static SyncRunResult CompletePipelineContract()
    {
        var sonarr = new SyncInstanceResult(
            "tv",
            SupportedServices.Sonarr,
            [CustomFormats(), QualityProfiles(), SonarrNaming(), MediaManagement()]
        );
        var radarr = new SyncInstanceResult(
            "movies",
            SupportedServices.Radarr,
            [QualitySizes(), RadarrNaming()]
        );
        return new SyncRunResult([sonarr, radarr]);
    }

    private static CustomFormatPipelineResult CustomFormats()
    {
        var identity = new CustomFormatIdentity("cf-one", "First CF");
        var managed = new CustomFormatIdentity("cf-old", "Managed CF");
        var selection = new CustomFormatSourceInfo(
            CfSource.CfGroupExplicit,
            "Group",
            CfInclusionReason.Selected,
            ["Profile"]
        );

        CustomFormatOutcome[] outcomes =
        [
            new CustomFormatReferenceMismatchOutcome("missing-cf"),
            new CustomFormatGroupReferenceMismatchOutcome("missing-group"),
            new IncompatibleCustomFormatGroupOutcome("Wrong Group", "wrong-group"),
            new EmptyCustomFormatGroupOutcome("Empty Group", "empty-group"),
            new CustomFormatAdoptedOutcome(identity, 10),
            new CustomFormatAmbiguousMatchOutcome(
                identity,
                [new CustomFormatServiceMatch("Match", 11)]
            ),
            new CustomFormatStateConflictOutcome(identity, managed, 12),
            new CustomFormatCreateRejectedOutcome(identity),
            new CustomFormatUpdateRejectedOutcome(identity),
            new CustomFormatDeleteRejectedOutcome(identity),
        ];
        CustomFormatDelta[] deltas =
        [
            new CustomFormatCreateDelta(identity, selection),
            new CustomFormatUpdateDelta(
                identity,
                selection,
                [
                    new CustomFormatNameChanged(new ValueDelta<string>("Old", "New")),
                    new CustomFormatIncludeWhenRenamingChanged(new ValueDelta<bool>(false, true)),
                    new CustomFormatSpecificationAdded("Added"),
                    new CustomFormatSpecificationChanged("Changed"),
                    new CustomFormatSpecificationRemoved("Removed"),
                ]
            ),
            new CustomFormatDeleteDelta(managed),
        ];
        return new CustomFormatPipelineResult(
            completedResources: 1,
            incompleteResources: 1,
            outcomes,
            deltas
        );
    }

    private static QualityProfilePipelineResult QualityProfiles()
    {
        var guide = new GuideBackedQualityProfileIdentity(new MappingKey("qp-one", "Guide"));
        var user = new UserDefinedQualityProfileIdentity("User");
        var serviceMatch = new QualityProfileServiceMatch("Existing", 20);
        var existing = new QualityProfileCustomFormatReference("Existing CF", "cf-existing");
        var rejected = new QualityProfileCustomFormatReference("Rejected CF", "cf-rejected");

        QualityProfileOutcome[] outcomes =
        [
            new QualityProfileReferenceMismatchOutcome("missing-qp"),
            new QualityProfileDuplicateNameOutcome("Duplicate"),
            new QualityProfileScoreCollisionOutcome(existing, rejected, 21),
            new QualityProfileNotFoundOutcome(guide),
            new QualityProfileAdoptedOutcome(user, 22),
            new QualityProfileMinimumScoreUnsatisfiedOutcome(guide, 10, 5, 9),
            new QualityProfileInvalidCutoffOutcome(guide, "Invalid"),
            new QualityProfileUnavailableCutoffOutcome(guide, "Unavailable"),
            new QualityProfileQualitiesRequiredOutcome(guide),
            new QualityProfileQualityReferenceMismatchOutcome(guide, ["Unknown"]),
            new QualityProfileResetScoreReferenceMismatchOutcome(guide, ["Missing"], ["Pattern"]),
            new QualityProfileRenameBlockedOutcome(guide, serviceMatch),
            new QualityProfileAmbiguousMatchOutcome(guide, [serviceMatch]),
            new QualityProfileCreateRejectedOutcome(guide),
            new QualityProfileUpdateRejectedOutcome(user),
        ];

        var currentLayout = new QualityProfileQuality("Current", true);
        var desiredLayout = new QualityProfileQualityGroup("Desired", true, ["One", "Two"]);
        var state = new QualityProfileControlledState(
            "Guide",
            null,
            "Cutoff",
            100,
            0,
            null,
            "English",
            [desiredLayout],
            [new QualityProfileCustomFormatScore("CF", "cf-one", 10)]
        );
        QualityProfileDelta[] deltas =
        [
            new QualityProfileCreateDelta(guide, state),
            new QualityProfileUpdateDelta(
                user,
                [
                    new QualityProfileNameChanged(new ValueDelta<string>("Old", "New")),
                    new QualityProfileUpgradeAllowedChanged(new ValueDelta<bool?>(null, false)),
                    new QualityProfileUpgradeUntilQualityChanged(
                        new ValueDelta<string?>(null, "Bluray")
                    ),
                    new QualityProfileUpgradeUntilScoreChanged(new ValueDelta<int?>(0, null)),
                    new QualityProfileMinimumFormatScoreChanged(new ValueDelta<int?>(null, 5)),
                    new QualityProfileMinimumUpgradeFormatScoreChanged(
                        new ValueDelta<int?>(null, 6)
                    ),
                    new QualityProfileLanguageChanged(new ValueDelta<string?>(null, "English")),
                    new QualityProfileQualityLayoutChanged([currentLayout], [desiredLayout]),
                    new QualityProfileCustomFormatScoreChanged(
                        "CF",
                        "cf-one",
                        new ValueDelta<int>(0, 10),
                        QualityProfileScoreChangeReason.Set
                    ),
                    new QualityProfileCustomFormatScoreChanged(
                        "Reset CF",
                        null,
                        new ValueDelta<int>(5, 0),
                        QualityProfileScoreChangeReason.Reset
                    ),
                ]
            ),
        ];
        return new QualityProfilePipelineResult(
            completedResources: 1,
            incompleteResources: 1,
            outcomes,
            deltas
        );
    }

    private static QualitySizePipelineResult QualitySizes()
    {
        var one = new QualitySizeValue.Numeric(1.25m);
        var two = new QualitySizeValue.Numeric(2.5m);
        var unlimited = new QualitySizeValue.Unlimited();
        QualitySizeOutcome[] outcomes =
        [
            new QualitySizeDefinitionReferenceMismatchOutcome("movie"),
            new QualitySizeReferenceMismatchOutcome("Unknown", "movie"),
            new QualitySizeServiceQualityNotFoundOutcome("Missing"),
            new QualitySizePreferredRatioClampedOutcome(new ValueDelta<decimal>(0.5m, 1m)),
            new QualitySizeMinimumGreaterThanPreferredOutcome("One", two, one),
            new QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome("Two", unlimited, two),
            new QualitySizePreferredGreaterThanMaximumOutcome("Three", two, one),
        ];
        QualitySizeDelta[] deltas =
        [
            new QualitySizeDelta(
                "Bluray",
                [
                    new QualitySizeMinimumChanged(new ValueDelta<QualitySizeValue>(one, two)),
                    new QualitySizePreferredChanged(
                        new ValueDelta<QualitySizeValue>(two, unlimited)
                    ),
                    new QualitySizeMaximumChanged(new ValueDelta<QualitySizeValue>(unlimited, two)),
                ]
            ),
        ];
        return new QualitySizePipelineResult(
            completedResources: 1,
            incompleteResources: 1,
            outcomes,
            deltas
        );
    }

    private static SonarrNamingPipelineResult SonarrNaming() =>
        new(
            completedFields: 1,
            incompleteFields: 1,
            Enum.GetValues<SonarrNamingFormatField>()
                .Select(x =>
                    (SonarrNamingOutcome)new SonarrNamingReferenceMismatchOutcome(x, x.ToString())
                )
                .ToList(),
            new SonarrNamingDelta
            {
                RenameEpisodes = new ValueDelta<bool?>(null, false),
                SeriesFolderFormat = new ValueDelta<string?>(null, "Series"),
                SeasonFolderFormat = new ValueDelta<string?>("Old", null),
                StandardEpisodeFormat = new ValueDelta<string?>("Old", "Standard"),
                DailyEpisodeFormat = new ValueDelta<string?>("Old", "Daily"),
                AnimeEpisodeFormat = new ValueDelta<string?>("Old", "Anime"),
            }
        );

    private static RadarrNamingPipelineResult RadarrNaming() =>
        new(
            completedFields: 1,
            incompleteFields: 1,
            Enum.GetValues<RadarrNamingFormatField>()
                .Select(x =>
                    (RadarrNamingOutcome)new RadarrNamingReferenceMismatchOutcome(x, x.ToString())
                )
                .ToList(),
            new RadarrNamingDelta
            {
                RenameMovies = new ValueDelta<bool?>(true, false),
                StandardMovieFormat = new ValueDelta<string?>(null, "Standard"),
                MovieFolderFormat = new ValueDelta<string?>("Old", null),
            }
        );

    private static MediaManagementPipelineResult MediaManagement() =>
        new(
            SyncResultStatus.Failed,
            new MediaManagementDelta(
                new ValueDelta<PropersAndRepacksMode?>(null, PropersAndRepacksMode.DoNotPrefer)
            )
        );
}
