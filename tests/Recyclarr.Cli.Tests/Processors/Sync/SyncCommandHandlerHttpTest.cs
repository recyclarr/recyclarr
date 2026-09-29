using System.IO.Abstractions;
using Autofac;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Client.V1;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Serilog.Events;
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
        AddInstanceConfig(InstanceName);
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
            "[real-instance] Custom Format 'x264' (cf-x264) matches multiple service formats: "
            + "'x264' (ID 1), 'x264 copy' (ID 2)";
        const string adopted =
            "[real-instance] Adopted Custom Format 'x264' (cf-x264) from service ID 7";
        const string unreachable =
            "[other-instance] The service could not be reached. Check base_url and that the "
            + "service is up.";

        result.Should().Be(ExitStatus.Failed);
        ConsoleOutput
            .Should()
            .Contain("Sync Diagnostics")
            .And.Contain(ambiguous)
            .And.Contain(adopted)
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
                            new QualitySizeDelta(
                                "Bluray-1080p",
                                [
                                    new QualitySizeMinimumChanged(
                                        new ValueDelta<QualitySizeValue>(
                                            new QualitySizeValue.Numeric(50),
                                            new QualitySizeValue.Numeric(50.4m)
                                        )
                                    ),
                                ]
                            ),
                        ]
                    ),
                    NewPipelineResult.RadarrNaming(
                        new RadarrNamingDelta
                        {
                            StandardMovieFormat = new ValueDelta<string?>("{Movie}", "{Title}"),
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
                        new RadarrNamingDelta
                        {
                            RenameMovies = new ValueDelta<bool?>(false, true),
                            StandardMovieFormat = new ValueDelta<string?>("a", "b"),
                        }
                    ),
                    NewPipelineResult.MediaManagement(
                        new MediaManagementDelta(
                            new ValueDelta<PropersAndRepacksMode?>(
                                PropersAndRepacksMode.PreferAndUpgrade,
                                PropersAndRepacksMode.DoNotPrefer
                            )
                        )
                    ),
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
                        [new CustomFormatCreateDelta(new("cf-x265", "x265 (HD)"), groupSource)]
                    ),
                    NewPipelineResult.QualityProfiles([], [NewProfile("HD Bluray")]),
                    NewPipelineResult.QualitySizes(
                        [],
                        [
                            new QualitySizeDelta(
                                "Bluray-2160p",
                                [
                                    new QualitySizeMaximumChanged(
                                        new ValueDelta<QualitySizeValue>(
                                            new QualitySizeValue.Numeric(400),
                                            new QualitySizeValue.Unlimited()
                                        )
                                    ),
                                ]
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
            .And.Contain("400 -> Unlimited")
            .And.MatchRegex(@"Radarr Media Naming \(Preview\) \[real-instance\] ──\s+No changes")
            .And.Contain("Not run: blocked by Custom Formats");
    }

    [Test]
    public async Task Diagnostics_list_errors_first_and_explain_pipelines_without_outcomes()
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
                    new CustomFormatGroupRequiredItemSelectedPlanningOutcome("group", "cf"),
                ]
            ),
        ]);

        await RunSync();

        const string blockingPlan =
            "[real-instance] Custom Format group 'missing-group' does not exist";
        const string blocked = "[real-instance] Quality Profiles blocked by Custom Formats";
        const string failedStatus = "[real-instance] Quality Sizes completed with status Failed";
        const string advisoryPlan =
            "[real-instance] Custom Format 'cf' is required by group 'group' and does not need "
            + "to be selected";
        const string adopted =
            "[real-instance] Adopted Custom Format 'x264' (cf-x264) from service ID 7";

        var errors = ConsoleOutput.IndexOf("Errors", StringComparison.Ordinal);
        var warnings = ConsoleOutput.IndexOf("Warnings", StringComparison.Ordinal);
        errors.Should().BeLessThan(warnings);
        foreach (var error in new[] { blockingPlan, blocked, failedStatus })
        {
            ConsoleOutput
                .IndexOf(error, StringComparison.Ordinal)
                .Should()
                .BeInRange(errors, warnings);
        }

        foreach (var warning in new[] { advisoryPlan, adopted })
        {
            ConsoleOutput
                .IndexOf(warning, StringComparison.Ordinal)
                .Should()
                .BeGreaterThan(warnings);
        }

        LogEntries
            .Should()
            .Contain((LogEventLevel.Error, blockingPlan))
            .And.Contain((LogEventLevel.Error, blocked))
            .And.Contain((LogEventLevel.Warning, advisoryPlan))
            .And.Contain((LogEventLevel.Warning, adopted));
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
    public async Task The_final_table_counts_every_kind_of_change_per_pipeline()
    {
        AddInstanceConfig(InstanceName);
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
                            new CustomFormatUpdateDelta(new("cf2", "Two"), source, []),
                            new CustomFormatDeleteDelta(new("cf3", "Three")),
                        ]
                    ),
                    NewPipelineResult.QualityProfiles(
                        [],
                        [NewProfile("HD"), NewProfileUpdate("UHD")]
                    ),
                    NewPipelineResult.QualitySizes(
                        [],
                        [new QualitySizeDelta("Bluray-1080p", []), new QualitySizeDelta("WEB", [])]
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
                    NewPipelineResult.QualityProfiles([], [NewProfileUpdate("UHD")]),
                    NewPipelineResult.MediaManagement(
                        new MediaManagementDelta(
                            new ValueDelta<PropersAndRepacksMode?>(
                                PropersAndRepacksMode.PreferAndUpgrade,
                                PropersAndRepacksMode.DoNotPrefer
                            )
                        )
                    ),
                ]
            ),
        ]);

        await RunSync(new CreateSyncJobRequest { Preview = true });

        ConsoleOutput
            .Should()
            .Contain("UHD (Change Reason: Changed)")
            .And.MatchRegex(@"Minimum Format Score\s+│\s+0\s+│\s+10")
            .And.Contain("✓ Remux-2160p")
            .And.Contain("✗ Remux-2160p")
            .And.MatchRegex(@"x265 \(HD\)\s+│\s+0\s+│\s+-10000\s+│\s+Set")
            .And.MatchRegex(
                @"Download Propers and Repacks\s+│\s+PreferAndUpgrade\s+│\s+DoNotPrefer"
            );
    }

    [Test]
    public async Task Diagnostics_are_sorted_by_instance_then_message()
    {
        AddInstanceConfig(InstanceName);
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
            "[alpha] The service does not contain quality 'A'",
            "[alpha] The service does not contain quality 'B'",
            "[zeta] The service does not contain quality 'B'",
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
                "[real-instance] Quality profile 'HD' cannot be renamed because 'HD Old' already "
                    + "exists at service ID 4"
            )
            .And.Contain("[real-instance] Quality 'WEB' preferred 90 exceeds maximum 80")
            .And.Contain(
                "[real-instance] Media naming field 'StandardMovieFormat' references unknown "
                    + "format 'bogus'"
            );
    }

    private static QualityProfileUpdateDelta NewProfileUpdate(string name) =>
        new(
            new UserDefinedQualityProfileIdentity(name),
            [
                new QualityProfileMinimumFormatScoreChanged(new ValueDelta<int?>(0, 10)),
                new QualityProfileQualityLayoutChanged(
                    [new QualityProfileQuality("Remux-2160p", Allowed: true)],
                    [new QualityProfileQuality("Remux-2160p", Allowed: false)]
                ),
                new QualityProfileCustomFormatScoreChanged(
                    "x265 (HD)",
                    "cf-x265",
                    new ValueDelta<int>(0, -10000),
                    QualityProfileScoreChangeReason.Set
                ),
            ]
        );

    private static QualityProfileCreateDelta NewProfile(string name) =>
        new(
            new UserDefinedQualityProfileIdentity(name),
            new QualityProfileControlledState(
                name,
                upgradeAllowed: true,
                upgradeUntilQuality: "Bluray-1080p",
                upgradeUntilScore: 10000,
                minimumFormatScore: 0,
                minimumUpgradeFormatScore: 1,
                language: null,
                [new QualityProfileQuality("Bluray-1080p", Allowed: true)],
                [new QualityProfileCustomFormatScore("x264", "cf-x264", 50)]
            )
        );

    private static CustomFormatCreateDelta Create(string trashId, string name) =>
        new(
            new CustomFormatIdentity(trashId, name),
            new CustomFormatSourceInfo(CfSource.FlatConfig, null, CfInclusionReason.None, [])
        );

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
            TestContext.CurrentContext.CancellationToken
        );

        response.IsSuccessful.Should().BeTrue();

        // non-null: a successful response always carries the job list
        return response.Content!.Jobs;
    }
}
