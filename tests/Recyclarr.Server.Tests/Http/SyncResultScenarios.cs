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
            [
                CustomFormats(),
                QualityProfiles(SupportedServices.Sonarr),
                SonarrNaming(),
                MediaManagement(),
            ]
        );
        var radarr = new SyncInstanceResult(
            "movies",
            SupportedServices.Radarr,
            [QualityProfiles(SupportedServices.Radarr), QualitySizes(), RadarrNaming()]
        );
        return new SyncRunResult([sonarr, radarr]);
    }

    public static SyncRunResult CompletePlanningOutcomeContract()
    {
        var instance = new SyncInstanceResult(
            "planning",
            SupportedServices.Sonarr,
            [],
            planningOutcomes: PlanningOutcomeContractInventory()
        );
        return new SyncRunResult([instance]);
    }

    private static IReadOnlyList<PlanningOutcome> PlanningOutcomeContractInventory() =>
        [
            new CustomFormatReferenceMismatchPlanningOutcome("missing-cf"),
            new CustomFormatGroupReferenceMismatchPlanningOutcome("missing-group"),
            new CustomFormatGroupSkipReferenceMismatchPlanningOutcome("missing-skip"),
            new CustomFormatGroupSelectReferenceMismatchPlanningOutcome(
                "select-group",
                "missing-select"
            ),
            new CustomFormatGroupExcludeReferenceMismatchPlanningOutcome(
                "exclude-group",
                "missing-exclude"
            ),
            new CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcome(
                "profile-group",
                "missing-profile"
            ),
            new CustomFormatQualityProfileReferenceAmbiguousPlanningOutcome(
                "direct-profile",
                ["Direct A", "Direct B"]
            ),
            new CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcome(
                "ambiguous-group",
                "group-profile",
                ["Group A", "Group B"]
            ),
            new CustomFormatGroupRequiredItemSelectedPlanningOutcome(
                "required-select-group",
                "required-select"
            ),
            new CustomFormatGroupDefaultItemSelectedPlanningOutcome(
                "default-select-group",
                "default-select"
            ),
            new CustomFormatGroupRequiredItemExcludedPlanningOutcome(
                "required-exclude-group",
                "required-exclude"
            ),
            new CustomFormatGroupNonDefaultItemExcludedPlanningOutcome(
                "non-default-exclude-group",
                "non-default-exclude"
            ),
        ];

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
            new IncompatibleCustomFormatGroupOutcome("Wrong Group", "wrong-group"),
            new EmptyCustomFormatGroupOutcome("Empty Group", "empty-group"),
            new CustomFormatDuplicateNameOutcome("Shared", ["cf-a", "cf-b"]),
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
        PlannedCustomFormatResource[] resources =
        [
            new()
            {
                Action = PlannedAction.Create,
                Identity = identity,
                SelectionProvenance = selection,
                Name = PlannedValue.Created("First CF"),
                IncludeWhenRenaming = PlannedValue.Created(false),
                Specifications = [new("Added", CustomFormatSpecificationChange.Added)],
            },
            new()
            {
                Action = PlannedAction.Update,
                Identity = identity,
                SelectionProvenance = selection,
                Name = PlannedValue.Compared("Old", "New"),
                IncludeWhenRenaming = PlannedValue.Compared(false, true),
                Specifications =
                [
                    new("Added", CustomFormatSpecificationChange.Added),
                    new("Changed", CustomFormatSpecificationChange.Changed),
                    new("Removed", CustomFormatSpecificationChange.Removed),
                    new("Kept", CustomFormatSpecificationChange.Unchanged),
                ],
            },
            new()
            {
                Action = PlannedAction.Unchanged,
                Identity = identity,
                SelectionProvenance = selection,
                Name = PlannedValue.Compared("Same", "Same"),
                IncludeWhenRenaming = PlannedValue.Compared(true, true),
                Specifications = [new("Kept", CustomFormatSpecificationChange.Unchanged)],
            },
        ];
        return new CustomFormatPipelineResult(
            completedResources: 1,
            incompleteResources: 1,
            outcomes,
            resources,
            [managed]
        );
    }

    // Radarr results add the Radarr-only language to the create state and the update components.
    private static QualityProfilePipelineResult QualityProfiles(SupportedServices service)
    {
        var guide = new GuideBackedQualityProfileIdentity(new MappingKey("qp-one", "Guide"));
        var user = new UserDefinedQualityProfileIdentity("User");
        var serviceMatch = new QualityProfileServiceMatch("Existing", 20);

        QualityProfileOutcome[] outcomes =
        [
            new QualityProfileReferenceMismatchOutcome("missing-qp"),
            new QualityProfileDuplicateNameOutcome("Duplicate"),
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

        var currentLayout = new QualityProfileQuality { Name = "Current", Allowed = true };
        var desiredLayout = new QualityProfileQualityGroup
        {
            Name = "Desired",
            Allowed = true,
            Qualities = ["One", "Two"],
        };
        var created = new PlannedQualityProfileResource
        {
            Action = PlannedAction.Create,
            Identity = guide,
            Name = PlannedValue.Created("Guide"),
            UpgradeAllowed = QualityProfileField.Of(
                managed: false,
                PlannedValue.Created<bool?>(null)
            ),
            UpgradeUntilQuality = QualityProfileField.Of(
                managed: true,
                PlannedValue.Created<string?>("Cutoff")
            ),
            UpgradeUntilScore = QualityProfileField.Of(
                managed: true,
                PlannedValue.Created<int?>(100)
            ),
            MinimumFormatScore = QualityProfileField.Of(
                managed: true,
                PlannedValue.Created<int?>(0)
            ),
            MinimumUpgradeFormatScore = QualityProfileField.Of(
                managed: true,
                PlannedValue.Created<int?>(null)
            ),
            Qualities = QualityProfileField.Of(
                managed: true,
                PlannedValue.Created<IReadOnlyList<QualityProfileQualityLayoutItem>>([
                    desiredLayout,
                ])
            ),
            CustomFormatScores =
            [
                new("CF", "cf-one", PlannedValue.Created(10), QualityProfileScoreReason.Set),
            ],
        };
        var updated = new PlannedQualityProfileResource
        {
            Action = PlannedAction.Update,
            Identity = user,
            Name = PlannedValue.Compared("Old", "New"),
            UpgradeAllowed = QualityProfileField.Of(
                managed: true,
                PlannedValue.Compared<bool?>(null, false)
            ),
            UpgradeUntilQuality = QualityProfileField.Of(
                managed: true,
                PlannedValue.Compared<string?>(null, "Bluray")
            ),
            UpgradeUntilScore = QualityProfileField.Of(
                managed: true,
                PlannedValue.Compared<int?>(0, null)
            ),
            MinimumFormatScore = QualityProfileField.Of(
                managed: true,
                PlannedValue.Compared<int?>(null, 5)
            ),
            MinimumUpgradeFormatScore = QualityProfileField.Of(
                managed: false,
                PlannedValue.Compared<int?>(6, 6)
            ),
            Qualities = QualityProfileField.Of(
                managed: true,
                PlannedValue.Compared<IReadOnlyList<QualityProfileQualityLayoutItem>>(
                    [currentLayout],
                    [desiredLayout],
                    changed: true
                )
            ),
            CustomFormatScores =
            [
                new("CF", "cf-one", PlannedValue.Compared(0, 10), QualityProfileScoreReason.Set),
                new("Reset CF", null, PlannedValue.Compared(5, 0), QualityProfileScoreReason.Reset),
                new("Mine", null, PlannedValue.Compared(7, 7), QualityProfileScoreReason.Unmanaged),
            ],
        };
        var isRadarr = service == SupportedServices.Radarr;
        PlannedQualityProfileResource[] resources = isRadarr
            ?
            [
                new RadarrPlannedQualityProfileResource(
                    created,
                    QualityProfileField.Of(managed: true, PlannedValue.Created<string?>("English"))
                ),
                new RadarrPlannedQualityProfileResource(
                    updated,
                    QualityProfileField.Of(
                        managed: true,
                        PlannedValue.Compared<string?>(null, "English")
                    )
                ),
            ]
            :
            [
                new SonarrPlannedQualityProfileResource(
                    created,
                    [
                        PlannedQualitySizeResource.Created(
                            "HDTV-1080p",
                            new QualitySizeValue.Numeric(5),
                            new QualitySizeValue.Numeric(30),
                            new QualitySizeValue.Unlimited()
                        ),
                    ]
                ),
                new SonarrPlannedQualityProfileResource(
                    updated,
                    [
                        PlannedQualitySizeResource.Compared(
                            "HDTV-1080p",
                            (new QualitySizeValue.Numeric(1), new QualitySizeValue.Numeric(5)),
                            (new QualitySizeValue.Unlimited(), new QualitySizeValue.Unlimited()),
                            (new QualitySizeValue.Unlimited(), new QualitySizeValue.Unlimited())
                        ),
                    ]
                ),
            ];
        if (!isRadarr)
        {
            outcomes =
            [
                .. outcomes,
                new QualityProfileQualitySizeOutcome(
                    guide,
                    new QualitySizeGuideReferenceMismatchOutcome("guide", "qs-missing")
                ),
                new QualityProfileQualityDefinitionUnusedOutcome(),
            ];
        }
        else
        {
            outcomes = [.. outcomes, new QualityProfileQualityDefinitionIgnoredOutcome(guide)];
        }
        return new QualityProfilePipelineResult(
            completedResources: 1,
            incompleteResources: 1,
            outcomes,
            resources
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
            new QualitySizePreferredRatioClampedOutcome(0.5m, 1m),
            new QualitySizeMinimumGreaterThanPreferredOutcome("One", two, one),
            new QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome("Two", unlimited, two),
            new QualitySizePreferredGreaterThanMaximumOutcome("Three", two, one),
            new QualitySizeGuideReferenceMismatchOutcome("guide", "qs-missing"),
            new QualitySizeAmbiguousGuideReferenceOutcome(["series", "anime"]),
        ];
        PlannedQualitySizeResource[] resources =
        [
            PlannedQualitySizeResource.Compared(
                "Bluray",
                (one, two),
                (two, unlimited),
                (unlimited, two)
            ),
            PlannedQualitySizeResource.Compared("WEB", (one, one), (two, two), (two, two)),
        ];
        return new QualitySizePipelineResult(
            completedResources: 1,
            incompleteResources: 1,
            outcomes,
            resources
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
            new PlannedSonarrNamingResource
            {
                Action = PlannedAction.Update,
                RenameEpisodes = PlannedValue.Compared<bool?>(null, false),
                SeriesFolderFormat = PlannedValue.Compared<string?>(null, "Series"),
                SeasonFolderFormat = PlannedValue.Compared<string?>("Old", null),
                StandardEpisodeFormat = PlannedValue.Compared<string?>("Old", "Standard"),
                DailyEpisodeFormat = PlannedValue.Compared<string?>("Daily", "Daily"),
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
            new PlannedRadarrNamingResource
            {
                Action = PlannedAction.Update,
                RenameMovies = PlannedValue.Compared<bool?>(true, false),
                StandardMovieFormat = PlannedValue.Compared<string?>(null, "Standard"),
                MovieFolderFormat = PlannedValue.Compared<string?>("Old", null),
            }
        );

    private static MediaManagementPipelineResult MediaManagement() =>
        new(
            SyncResultStatus.Failed,
            new PlannedMediaManagementResource
            {
                Action = PlannedAction.Update,
                PropersAndRepacks = PlannedValue.Compared<PropersAndRepacksMode?>(
                    null,
                    PropersAndRepacksMode.DoNotPrefer
                ),
            }
        );
}
