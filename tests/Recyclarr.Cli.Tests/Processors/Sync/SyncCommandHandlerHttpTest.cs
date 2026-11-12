using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Client.V1;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Platform;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TestLibrary.Autofac;
using Serilog.Events;
using QualityProfileScoreReason = Recyclarr.Pipelines.QualityProfile.QualityProfileScoreReason;
using SupportedServices = Recyclarr.TrashGuide.SupportedServices;

namespace Recyclarr.Cli.Tests.Processors.Sync;

/// <summary>
/// Drives the real command handler against a real in-process server over HTTP. Everything the CLI
/// observes here (job creation, polling, terminal status) travels the same wire it does in
/// production; only the sync engine behind the server is stubbed.
/// </summary>
internal sealed class SyncCommandHandlerHttpTest : CliServerHttpFixture
{
    private const string InstanceName = "real-instance";

    /// <summary>
    /// The ANSI "erase line" control sequence; the live display emits it to redraw its table.
    /// </summary>
    private const string EraseLine = "\u001b[2K";

    private static readonly SyncInstanceResult Succeeded = new(
        InstanceName,
        SupportedServices.Radarr,
        []
    );

    private static readonly SyncInstanceResult Failed = new(
        "other-instance",
        SupportedServices.Radarr,
        [],
        failure: new ServiceUnavailableFailure()
    );

    /// <summary>
    /// Read lazily by the substitute below, so each test decides how the sync "went" before it
    /// triggers a job.
    /// </summary>
    private SyncRunResult _result = new([Succeeded]);

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterMockFor<ISyncOrchestrator>(m =>
            m.RunAsync(
                    Arg.Any<IReadOnlyList<IServiceConfiguration>>(),
                    Arg.Any<ISyncSettings>(),
                    Arg.Any<IInstanceSyncProgress>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(_ => Task.FromResult(_result))
        );
    }

    [Test]
    public async Task A_sync_that_succeeded_exits_successfully()
    {
        AddInstanceConfig(InstanceName);

        var result = await RunSync();

        result.Should().Be(ExitStatus.Succeeded);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Succeeded");
    }

    /// <summary>
    /// A partial sync applied everything it could, which the CLI has always reported as success.
    /// </summary>
    [Test]
    public async Task A_sync_that_was_only_partial_still_exits_successfully()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([Succeeded, Failed]);

        var result = await RunSync();

        result.Should().Be(ExitStatus.Succeeded);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Partial");
    }

    [Test]
    public async Task A_sync_that_failed_exits_with_failure()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([Failed]);

        var result = await RunSync();

        result.Should().Be(ExitStatus.Failed);
        (await TerminalStatusOfTheOnlyJob()).Should().Be("Failed");
    }

    [Test]
    public async Task An_interactive_terminal_shows_live_instance_progress()
    {
        AddInstanceConfig(InstanceName);
        CliConsole.Profile.Capabilities.Interactive = true;
        CliConsole.EmitAnsiSequences = true;

        var result = await RunSync();

        result.Should().Be(ExitStatus.Succeeded);
        ConsoleOutput.Should().Contain("Legend:").And.Contain(EraseLine);
    }

    /// <summary>
    /// Live-display cursor operations can fail on a redirected stream (notably on Windows), so a
    /// non-interactive console must reach the results without them.
    /// </summary>
    [Test]
    public async Task A_non_interactive_console_shows_results_without_live_progress()
    {
        AddInstanceConfig(InstanceName);
        CliConsole.EmitAnsiSequences = true;

        var result = await RunSync();

        result.Should().Be(ExitStatus.Succeeded);
        ConsoleOutput
            .Should()
            .NotContain("Legend:")
            .And.NotContain(EraseLine)
            .And.Contain(InstanceName);
    }

    /// <summary>
    /// Log mode (explicit or from redirected output) discards console output, but live-display
    /// cursor operations bypass that writer, so the console must stop claiming to be interactive.
    /// </summary>
    [Test]
    public async Task Log_mode_syncs_without_an_interactive_console()
    {
        AddInstanceConfig(InstanceName);
        CliConsole.Profile.Capabilities.Interactive = true;

        var exitCode = await RunCliAsync("sync", "--log");

        exitCode.Should().Be((int)ExitStatus.Succeeded);
        CliConsole.Profile.Capabilities.Interactive.Should().BeFalse();
        LogOutput.Should().Contain("Instance real-instance finished with status Succeeded");
    }

    /// <summary>
    /// The server refuses a request naming an unconfigured instance and never creates a job.
    /// </summary>
    [Test]
    public async Task A_refused_request_fails_the_command_and_leaves_no_job_to_poll()
    {
        AddInstanceConfig(InstanceName);

        var result = await RunSync(new CreateSyncJobRequest { Instances = ["no-such-instance"] });

        result.Should().Be(ExitStatus.Failed);
        (await ListJobs()).Should().BeEmpty();
    }

    [Test]
    public async Task Outcomes_appear_in_the_diagnostics_panel_and_the_log_with_their_instance()
    {
        AddInstanceConfig(InstanceName, Failed.InstanceName);
        var identity = new CustomFormatIdentity("cf-x264", "x264");
        _result = new SyncRunResult(
            [
                new SyncInstanceResult(
                    InstanceName,
                    SupportedServices.Radarr,
                    [
                        NewPipelineResult.CustomFormats(
                            [
                                new CustomFormatAmbiguousMatchOutcome(
                                    identity,
                                    [new("x264", 1), new("x264 copy", 2)]
                                ),
                                new CustomFormatAdoptedOutcome(identity, 7),
                                new CustomFormatDuplicateNameOutcome("Shared", ["cf-a", "cf-b"]),
                            ],
                            [],
                            failed: 1
                        ),
                    ],
                    fault: new SyncFault("instance-ref")
                ),
                Failed,
            ],
            new SyncFault("run-ref")
        );

        var result = await RunSync();

        const string ambiguous =
            "[real-instance] Custom Format 'x264' (cf-x264) cannot sync because multiple service "
            + "formats match its name: 'x264' (ID 1), 'x264 copy' (ID 2)";
        const string adopted =
            "[real-instance] Custom Format 'x264' (cf-x264) already existed in the service (ID 7)";
        const string duplicateName =
            "[real-instance] Custom Format name 'Shared' is used by multiple trash IDs (cf-a, "
            + "cf-b), so none of them sync";
        const string unreachable =
            "[other-instance] The service could not be reached. Check base_url and that the "
            + "service is up.";

        result.Should().Be(ExitStatus.Failed);
        ConsoleOutput
            .Should()
            .Contain("Sync Diagnostics")
            .And.Contain(ambiguous)
            .And.Contain(adopted)
            .And.Contain(duplicateName)
            .And.Contain(unreachable)
            .And.Contain("[real-instance] Unexpected sync fault. Reference: instance-ref")
            .And.Contain("• Unexpected sync fault. Reference: run-ref")
            .And.MatchRegex(@"✗\s+other-instance\s+--\s+--\s+--\s+--\s+--");
        LogOutput
            .Should()
            .Contain(ambiguous)
            .And.Contain(adopted)
            .And.Contain(unreachable)
            .And.Contain("Unexpected sync fault. Reference: run-ref");
    }

    [Test]
    public async Task A_sync_without_outcomes_shows_no_diagnostics_panel()
    {
        AddInstanceConfig(InstanceName);

        await RunSync();

        ConsoleOutput.Should().NotContain("Sync Diagnostics");
    }

    [Test]
    public async Task The_final_table_counts_changes_and_marks_blocked_pipelines_as_not_run()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.CustomFormats(
                        [],
                        [Create("cf1", "One"), Create("cf2", "Two")]
                    ),
                    NewPipelineResult.Blocked(
                        NewPipelineResult.QualityProfiles([], []),
                        PipelineType.CustomFormat
                    ),
                ]
            ),
        ]);

        await RunSync();

        ConsoleOutput.Should().MatchRegex(@"real-instance\s+2\s+--");
    }

    [Test]
    public async Task Preview_shows_each_change_with_its_current_and_desired_value()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.CustomFormats([], [Create("cf1", "Bad Dual Groups")]),
                    NewPipelineResult.QualitySizes(
                        [],
                        [
                            Size(
                                "Bluray-1080p",
                                new QualitySizeValue.Numeric(50),
                                new QualitySizeValue.Numeric(50.4m)
                            ),
                        ]
                    ),
                    NewPipelineResult.RadarrNaming(
                        new PlannedRadarrNamingResource
                        {
                            Action = PlannedAction.Update,
                            StandardMovieFormat = PlannedValue.Compared<string?>(
                                "{Movie}",
                                "{Title}"
                            ),
                        }
                    ),
                ]
            ),
        ]);

        await RunSync(new CreateSyncJobRequest { Preview = true });

        ConsoleOutput
            .Should()
            .Contain("Custom Format (Preview) [real-instance]")
            .And.Contain("Bad Dual Groups")
            .And.Contain("50 -> 50.4")
            .And.MatchRegex(@"Movie\s+│?\s*\{Movie\}\s+│?\s*\{Title\}");
    }

    [Test]
    public async Task A_normal_run_shows_no_change_sections()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [NewPipelineResult.CustomFormats([], [Create("cf1", "Bad Dual Groups")])]
            ),
        ]);

        await RunSync();

        ConsoleOutput.Should().NotContain("(Preview)").And.NotContain("Bad Dual Groups");
    }

    [Test]
    public async Task The_final_table_marks_each_pipeline_status_and_counts_its_changes()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.CustomFormats([], []),
                    NewPipelineResult.QualityProfiles(
                        [new QualityProfileDuplicateNameOutcome("HD")],
                        [NewProfile("HD")],
                        failed: 1
                    ),
                    NewPipelineResult.QualitySizes([], [], failed: 1),
                    NewPipelineResult.RadarrNaming(
                        new PlannedRadarrNamingResource
                        {
                            Action = PlannedAction.Update,
                            RenameMovies = PlannedValue.Compared<bool?>(false, true),
                            StandardMovieFormat = PlannedValue.Compared<string?>("a", "b"),
                            MovieFolderFormat = PlannedValue.Compared<string?>("c", "c"),
                        }
                    ),
                    NewPipelineResult.MediaManagement(PropersAndRepacks(PlannedAction.Update)),
                ]
            ),
        ]);

        await RunSync();

        ConsoleOutput.Should().MatchRegex(@"~\s+real-instance\s+✓\s+~\s+✗\s+2\s+1");
    }

    [Test]
    public async Task An_instance_without_results_keeps_its_progress_status_in_the_final_table()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                $"""
                radarr:
                  {InstanceName}:
                    base_url: http://localhost:7878
                    api_key: asdf
                  second-instance:
                    base_url: http://localhost:7879
                    api_key: asdf
                """
            )
        );

        await RunSync();

        ConsoleOutput.Should().MatchRegex(@"--\s+second-instance\s+--\s+--\s+--\s+--\s+--");
    }

    [Test]
    public async Task Preview_renders_every_pipeline_section()
    {
        AddInstanceConfig(InstanceName);
        var groupSource = new CustomFormatSourceInfo(
            CfSource.CfGroupImplicit,
            "Golden Rule",
            CfInclusionReason.Required,
            ["HD Bluray"]
        );

        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.CustomFormats(
                        [],
                        [Cf(PlannedAction.Create, "cf-x265", "x265 (HD)", groupSource)]
                    ),
                    NewPipelineResult.QualityProfiles([], [NewProfile("HD Bluray")]),
                    NewPipelineResult.QualitySizes(
                        [],
                        [
                            Size(
                                "Bluray-2160p",
                                new QualitySizeValue.Numeric(1),
                                new QualitySizeValue.Numeric(1),
                                new QualitySizeValue.Numeric(400),
                                new QualitySizeValue.Unlimited()
                            ),
                        ]
                    ),
                    NewPipelineResult.RadarrNaming(null),
                    NewPipelineResult.Blocked(
                        NewPipelineResult.MediaManagement(null),
                        PipelineType.CustomFormat
                    ),
                ]
            ),
        ]);

        await RunSync(new CreateSyncJobRequest { Preview = true });

        ConsoleOutput
            .Should()
            .Contain("(from group: Golden Rule [implicit via: HD Bluray])")
            .And.MatchRegex(@"Create\s+│\s+x265 \(HD\)\s+│\s+cf-x265\s+│\s+required")
            .And.Contain("HD Bluray (Change Reason: New)")
            .And.Contain("Profile Updates")
            .And.Contain("Quality Updates")
            .And.MatchRegex(@"x264\s+│\s+UNSET\s+│\s+50\s+│\s+Set")
            .And.NotContain("Schema Default")
            .And.Contain("400 -> Unlimited")
            .And.MatchRegex(@"Radarr Media Naming \(Preview\) \[real-instance\] ──\s+No changes")
            .And.Contain("Not run: blocked by Custom Formats");
    }

    [Test]
    public async Task Diagnostics_list_by_severity_and_explain_pipelines_without_outcomes()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.CustomFormats(
                        [new CustomFormatAdoptedOutcome(new("cf-x264", "x264"), 7)],
                        []
                    ),
                    NewPipelineResult.Blocked(
                        NewPipelineResult.QualityProfiles([], []),
                        PipelineType.CustomFormat
                    ),
                    NewPipelineResult.QualitySizes([], [], failed: 1),
                ],
                planningOutcomes:
                [
                    new CustomFormatGroupReferenceMismatchPlanningOutcome("missing-group"),
                    new CustomFormatGroupRequiredItemExcludedPlanningOutcome("group", "cf"),
                ]
            ),
        ]);

        await RunSync();

        const string blockingPlan =
            "[real-instance] Custom Format group 'missing-group' does not exist in the guide";
        const string blocked = "[real-instance] Quality Profiles blocked by Custom Formats";
        const string failedStatus = "[real-instance] Quality Sizes completed with status Failed";
        const string advisoryPlan =
            "[real-instance] Excluding required Custom Format 'cf' from group 'group' has no "
            + "effect (required formats are always included)";
        const string adopted =
            "[real-instance] Custom Format 'x264' (cf-x264) already existed in the service (ID "
            + "7); Recyclarr now manages it";

        var errors = ConsoleOutput.IndexOf("Errors", StringComparison.Ordinal);
        var warnings = ConsoleOutput.IndexOf("Warnings", StringComparison.Ordinal);
        var information = ConsoleOutput.IndexOf("Information", StringComparison.Ordinal);
        new[] { errors, warnings, information }.Should().BeInAscendingOrder();
        foreach (var error in new[] { blockingPlan, blocked, failedStatus })
        {
            ConsoleOutput
                .IndexOf(error, StringComparison.Ordinal)
                .Should()
                .BeInRange(errors, warnings);
        }

        ConsoleOutput
            .IndexOf(advisoryPlan, StringComparison.Ordinal)
            .Should()
            .BeInRange(warnings, information);
        ConsoleOutput
            .IndexOf(adopted, StringComparison.Ordinal)
            .Should()
            .BeGreaterThan(information);

        LogEntries
            .Should()
            .Contain((LogEventLevel.Error, blockingPlan))
            .And.Contain((LogEventLevel.Error, blocked))
            .And.Contain((LogEventLevel.Warning, advisoryPlan))
            .And.Contain((LogEventLevel.Information, adopted));
    }

    [Test]
    public async Task Each_instance_logs_a_summary_of_its_changes()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [NewPipelineResult.CustomFormats([], [Create("cf1", "One"), Create("cf2", "Two")])]
            ),
        ]);

        await RunSync();

        LogEntries
            .Should()
            .Contain(
                (
                    LogEventLevel.Information,
                    "Instance real-instance finished with status Succeeded; changes: "
                        + "Custom Formats 2"
                )
            );
    }

    [Test]
    public async Task The_final_table_counts_every_kind_of_change_but_not_unchanged_items()
    {
        AddInstanceConfig(InstanceName);
        var one = new QualitySizeValue.Numeric(1);
        var two = new QualitySizeValue.Numeric(2);
        var source = new CustomFormatSourceInfo(
            CfSource.FlatConfig,
            null,
            CfInclusionReason.None,
            []
        );
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.CustomFormats(
                        [],
                        [
                            Create("cf1", "One"),
                            Cf(PlannedAction.Update, "cf2", "Two", source),
                            Cf(PlannedAction.Unchanged, "cf4", "Four", source),
                        ],
                        [new CustomFormatIdentity("cf3", "Three")]
                    ),
                    NewPipelineResult.QualityProfiles(
                        [],
                        [
                            NewProfile("HD"),
                            NewProfileUpdate("UHD"),
                            NewProfileUpdate("SD", PlannedAction.Unchanged),
                        ]
                    ),
                    NewPipelineResult.QualitySizes(
                        [],
                        [
                            Size("Bluray-1080p", one, two),
                            Size("WEB", one, two),
                            Size("HDTV", one, one),
                        ]
                    ),
                ]
            ),
        ]);

        await RunSync();

        ConsoleOutput.Should().MatchRegex(@"✓\s+real-instance\s+3\s+2\s+2\s+--\s+--");
    }

    [Test]
    public async Task Preview_shows_changed_profiles_and_media_management_as_current_and_new()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.QualityProfiles(
                        [],
                        [NewProfileUpdate("UHD"), NewProfileUpdate("SD", PlannedAction.Unchanged)]
                    ),
                    NewPipelineResult.MediaManagement(PropersAndRepacks(PlannedAction.Update)),
                ]
            ),
        ]);

        await RunSync(new CreateSyncJobRequest { Preview = true });

        ConsoleOutput
            .Should()
            .Contain("UHD (Change Reason: Changed)")
            .And.MatchRegex(@"Minimum Format Score\s+│\s+0\s+│\s+10")
            .And.MatchRegex(@"Language\s+│\s+English\s+│\s+French")
            .And.Contain("✓ Remux-2160p")
            .And.Contain("✗ Remux-2160p")
            .And.MatchRegex(@"x265 \(HD\)\s+│\s+0\s+│\s+-10000\s+│\s+Set")
            .And.MatchRegex(
                @"Download Propers and Repacks\s+│\s+PreferAndUpgrade\s+│\s+DoNotPrefer"
            );
        ConsoleOutput
            .Should()
            .NotContain("SD (Change Reason")
            .And.NotContain("Kept Score")
            .And.NotMatchRegex(@"Upgrade Until Score\s+│");
    }

    private static PlannedMediaManagementResource PropersAndRepacks(PlannedAction action) =>
        new()
        {
            Action = action,
            PropersAndRepacks = PlannedValue.Compared<PropersAndRepacksMode?>(
                action == PlannedAction.Update
                    ? PropersAndRepacksMode.PreferAndUpgrade
                    : PropersAndRepacksMode.DoNotPrefer,
                PropersAndRepacksMode.DoNotPrefer
            ),
        };

    [Test]
    public async Task Diagnostics_are_sorted_by_instance_then_message()
    {
        AddInstanceConfig("zeta", "alpha");
        _result = new SyncRunResult([
            new SyncInstanceResult(
                "zeta",
                SupportedServices.Radarr,
                [
                    NewPipelineResult.QualitySizes(
                        [new QualitySizeServiceQualityNotFoundOutcome("B")],
                        []
                    ),
                ]
            ),
            new SyncInstanceResult(
                "alpha",
                SupportedServices.Radarr,
                [
                    NewPipelineResult.QualitySizes(
                        [
                            new QualitySizeServiceQualityNotFoundOutcome("B"),
                            new QualitySizeServiceQualityNotFoundOutcome("A"),
                        ],
                        []
                    ),
                ]
            ),
        ]);

        await RunSync();

        string[] expectedOrder =
        [
            "[alpha] The service has no quality 'A'",
            "[alpha] The service has no quality 'B'",
            "[zeta] The service has no quality 'B'",
        ];

        expectedOrder
            .Select(x => ConsoleOutput.IndexOf(x, StringComparison.Ordinal))
            .Should()
            .BeInAscendingOrder()
            .And.NotContain(-1);
    }

    [Test]
    public async Task Profile_size_and_naming_outcomes_use_their_own_wording()
    {
        AddInstanceConfig(InstanceName);
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Radarr,
                [
                    NewPipelineResult.QualityProfiles(
                        [
                            new QualityProfileRenameBlockedOutcome(
                                new UserDefinedQualityProfileIdentity("HD"),
                                new QualityProfileServiceMatch("HD Old", 4)
                            ),
                        ],
                        [],
                        failed: 1
                    ),
                    NewPipelineResult.QualitySizes(
                        [
                            new QualitySizePreferredGreaterThanMaximumOutcome(
                                "WEB",
                                new QualitySizeValue.Numeric(90),
                                new QualitySizeValue.Numeric(80)
                            ),
                        ],
                        [],
                        failed: 1
                    ),
                    NewPipelineResult.RadarrNaming(
                        null,
                        [
                            new RadarrNamingReferenceMismatchOutcome(
                                RadarrNamingFormatField.StandardMovieFormat,
                                "bogus"
                            ),
                        ]
                    ),
                ]
            ),
        ]);

        await RunSync();

        ConsoleOutput
            .Should()
            .Contain(
                "[real-instance] Quality profile 'HD' cannot be renamed to 'HD Old' because a "
                    + "profile with that name already exists (ID 4)"
            )
            .And.Contain(
                "[real-instance] Quality 'WEB': preferred (90) cannot be greater than max (80)"
            )
            .And.Contain(
                "[real-instance] Media naming `standardMovieFormat` references unknown format "
                    + "'bogus'"
            );
    }

    [Test]
    public async Task Sonarr_v5_profile_sizes_render_in_preview_and_diagnostics()
    {
        AddInstanceConfig(InstanceName);
        var identity = new UserDefinedQualityProfileIdentity("Series");
        _result = new SyncRunResult([
            new SyncInstanceResult(
                InstanceName,
                SupportedServices.Sonarr,
                [
                    NewPipelineResult.QualityProfiles(
                        [
                            new QualityProfileQualitySizeOutcome(
                                identity,
                                new QualitySizeGuideReferenceMismatchOutcome("guide", "qs-x")
                            ),
                            new QualityProfileQualityDefinitionUnusedOutcome(),
                        ],
                        [
                            new SonarrPlannedQualityProfileResource(
                                NewProfileUpdate("Series", PlannedAction.Unchanged) with
                                {
                                    Action = PlannedAction.Update,
                                    Identity = identity,
                                },
                                [
                                    Size(
                                        "HDTV-1080p",
                                        new QualitySizeValue.Numeric(1),
                                        new QualitySizeValue.Numeric(5)
                                    ),
                                    Size(
                                        "WEB-1080p",
                                        new QualitySizeValue.Numeric(1),
                                        new QualitySizeValue.Numeric(1)
                                    ),
                                ]
                            ),
                        ],
                        failed: 1
                    ),
                ]
            ),
        ]);

        await RunSync(new CreateSyncJobRequest { Preview = true });

        ConsoleOutput
            .Should()
            .Contain("Quality Size Updates")
            .And.MatchRegex(@"HDTV-1080p\s+│\s+1 -> 5\s+│")
            .And.NotContain("WEB-1080p")
            .And.Contain(
                "[real-instance] Quality profile 'Series': The guide profile references a quality "
                    + "definition that does not exist: guide (qs-x)"
            )
            .And.Contain("`quality_definition` only applies to profiles listed under");
    }

    // Every caller syncs a Radarr instance, so profiles carry Radarr's language.
    private static RadarrPlannedQualityProfileResource NewProfileUpdate(
        string name,
        PlannedAction action = PlannedAction.Update
    )
    {
        var changed = action == PlannedAction.Update;
        return new RadarrPlannedQualityProfileResource(
            new PlannedQualityProfileResource
            {
                Action = action,
                Identity = new UserDefinedQualityProfileIdentity(name),
                Name = PlannedValue.Compared(name, name),
                UpgradeAllowed = Set(PlannedValue.Compared<bool?>(true, true)),
                UpgradeUntilQuality = Set(
                    PlannedValue.Compared<string?>("Remux-2160p", "Remux-2160p")
                ),
                UpgradeUntilScore = Set(PlannedValue.Compared<int?>(100, 100)),
                MinimumFormatScore = Set(PlannedValue.Compared<int?>(changed ? 0 : 10, 10)),
                MinimumUpgradeFormatScore = Set(PlannedValue.Compared<int?>(1, 1)),
                Qualities = Set(
                    PlannedValue.Compared<IReadOnlyList<QualityProfileQualityLayoutItem>>(
                        [new QualityProfileQuality { Name = "Remux-2160p", Allowed = true }],
                        [new QualityProfileQuality { Name = "Remux-2160p", Allowed = !changed }],
                        changed
                    )
                ),
                CustomFormatScores =
                [
                    new(
                        "x265 (HD)",
                        "cf-x265",
                        PlannedValue.Compared(changed ? 0 : -10000, -10000),
                        QualityProfileScoreReason.Set
                    ),
                    new(
                        "Kept Score",
                        null,
                        PlannedValue.Compared(5, 5),
                        QualityProfileScoreReason.Unmanaged
                    ),
                ],
            },
            Set(PlannedValue.Compared<string?>(changed ? "English" : "French", "French"))
        );
    }

    private static QualityProfileField<T> Set<T>(PlannedValue<T> value) =>
        QualityProfileField.Of(managed: true, value);

    private static RadarrPlannedQualityProfileResource NewProfile(string name) =>
        new(
            new PlannedQualityProfileResource
            {
                Action = PlannedAction.Create,
                Identity = new UserDefinedQualityProfileIdentity(name),
                Name = PlannedValue.Created(name),
                UpgradeAllowed = Set(PlannedValue.Created<bool?>(true)),
                UpgradeUntilQuality = Set(PlannedValue.Created<string?>("Bluray-1080p")),
                UpgradeUntilScore = Set(PlannedValue.Created<int?>(10000)),
                MinimumFormatScore = Set(PlannedValue.Created<int?>(0)),
                MinimumUpgradeFormatScore = Set(PlannedValue.Created<int?>(1)),
                Qualities = Set(
                    PlannedValue.Created<IReadOnlyList<QualityProfileQualityLayoutItem>>([
                        new QualityProfileQuality { Name = "Bluray-1080p", Allowed = true },
                    ])
                ),
                CustomFormatScores =
                [
                    new("x264", "cf-x264", PlannedValue.Created(50), QualityProfileScoreReason.Set),
                    new(
                        "Schema Default",
                        null,
                        PlannedValue.Created(0),
                        QualityProfileScoreReason.Unmanaged
                    ),
                ],
            },
            QualityProfileField.Of(managed: false, PlannedValue.Created<string?>(null))
        );

    private static PlannedCustomFormatResource Create(string trashId, string name) =>
        Cf(PlannedAction.Create, trashId, name);

    private static PlannedCustomFormatResource Cf(
        PlannedAction action,
        string trashId,
        string name,
        CustomFormatSourceInfo? source = null
    ) =>
        new()
        {
            Action = action,
            Identity = new CustomFormatIdentity(trashId, name),
            SelectionProvenance =
                source
                ?? new CustomFormatSourceInfo(
                    CfSource.FlatConfig,
                    null,
                    CfInclusionReason.None,
                    []
                ),
            Name =
                action == PlannedAction.Create
                    ? PlannedValue.Created(name)
                    : PlannedValue.Compared(name, name, action == PlannedAction.Update),
            IncludeWhenRenaming = PlannedValue.Compared(false, false),
            Specifications = [],
        };

    private static PlannedQualitySizeResource Size(
        string quality,
        QualitySizeValue currentMinimum,
        QualitySizeValue desiredMinimum,
        QualitySizeValue? currentMaximum = null,
        QualitySizeValue? desiredMaximum = null
    )
    {
        QualitySizeValue unlimited = new QualitySizeValue.Unlimited();
        return PlannedQualitySizeResource.Compared(
            quality,
            (currentMinimum, desiredMinimum),
            (unlimited, unlimited),
            (currentMaximum ?? unlimited, desiredMaximum ?? unlimited)
        );
    }

    private async Task<ExitStatus> RunSync(CreateSyncJobRequest? request = null)
    {
        return await ResolveCli<SyncCommandHandler>()
            .RunAsync(
                Api,
                request ?? new CreateSyncJobRequest(),
                TestContext.CurrentContext.CancellationToken
            );
    }

    private async Task<string> TerminalStatusOfTheOnlyJob()
    {
        var jobs = await ListJobs();
        return jobs.Should().ContainSingle().Subject.Status;
    }

    private async Task<IReadOnlyList<SyncJobSummaryResponse>> ListJobs()
    {
        using var response = await Api.JobsGet(
            status: null,
            trigger: null,
            TestContext.CurrentContext.CancellationToken
        );

        response.IsSuccessful.Should().BeTrue();

        // non-null: a successful response always carries the job list
        return response.Content!.Jobs;
    }
}
