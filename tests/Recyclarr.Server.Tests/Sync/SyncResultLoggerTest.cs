using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Sync.Results;
using Recyclarr.Sync.Results;
using Recyclarr.TrashGuide;
using Serilog.Events;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class SyncResultLoggerTest
{
    [Test]
    public void Completed_result_retains_run_instance_pipeline_and_resource_context()
    {
        var result = CreateResult();
        var log = new RecordingLogger();
        var jobId = JobId.New();

        new SyncResultLogger(log).Log(jobId, result);

        log.Events.Should().OnlyContain(evt => HasScalar(evt, "JobId", jobId.Value));
        var run = log
            .Events.Should()
            .ContainSingle(evt =>
                evt.MessageTemplate.Text.Equals(
                    "Sync job {JobId} completed with status {Status}",
                    StringComparison.Ordinal
                )
            )
            .Which;
        GetScalar(run, "Status").Should().Be(SyncResultStatus.Partial);
        log.Events.Should()
            .ContainSingle(evt => HasScalar(evt, "FaultReference", "fault-reference"));
        log.Events.Should()
            .ContainSingle(evt =>
                HasScalar(evt, "Instance", "offline")
                && HasScalar(evt, "FaultReference", "instance-fault")
            );
        log.Events.Should()
            .ContainSingle(evt =>
                HasScalar(evt, "Instance", "offline")
                && HasScalar(evt, "FailureType", nameof(ServiceUnavailableFailure))
            );
        var instanceSummary = log
            .Events.Should()
            .ContainSingle(evt =>
                evt.MessageTemplate.Text.StartsWith("Sync instance", StringComparison.Ordinal)
                && HasScalar(evt, "Instance", "tv")
            )
            .Which;
        GetScalar(instanceSummary, "Service").Should().Be(SupportedServices.Sonarr);
        GetScalar(instanceSummary, "Status").Should().Be(result.Instances[0].Status);

        var pipelineSummaries = log
            .Events.Where(evt =>
                evt.MessageTemplate.Text.StartsWith("Pipeline {Pipeline", StringComparison.Ordinal)
            )
            .ToList();
        pipelineSummaries
            .Select(evt => GetScalar(evt, "Pipeline"))
            .Should()
            .Equal(
                "CustomFormat",
                "QualityProfile",
                "QualitySize",
                "MediaNaming",
                "MediaManagement",
                "MediaNaming"
            );
        var customFormatSummary = pipelineSummaries
            .Should()
            .ContainSingle(evt => HasScalar(evt, "Pipeline", "CustomFormat"))
            .Which;
        GetScalar(customFormatSummary, "Status").Should().Be(SyncResultStatus.Succeeded);
        log.Events.Should()
            .ContainSingle(evt =>
                HasScalar(evt, "OutcomeType", nameof(CustomFormatAdoptedOutcome))
                && HasScalar(evt, "Pipeline", "CustomFormat")
                && PropertyContains(evt, "Outcome", "cf-one")
                && PropertyContains(evt, "Outcome", "First CF")
            );

        log.Events.Where(evt => evt.Properties.ContainsKey("DeltaType"))
            .Select(evt => GetScalar(evt, "DeltaType"))
            .Should()
            .Equal(
                nameof(CustomFormatCreateDelta),
                nameof(QualitySizeDelta),
                nameof(SonarrNamingDelta),
                nameof(MediaManagementDelta),
                nameof(RadarrNamingDelta)
            );
        log.Events.Should()
            .ContainSingle(evt =>
                HasScalar(evt, "DeltaType", nameof(CustomFormatCreateDelta))
                && PropertyContains(evt, "Delta", "cf-one")
                && PropertyContains(evt, "Delta", "First CF")
            );

        var blockingPlanningOutcome = log
            .Events.Should()
            .ContainSingle(evt =>
                HasScalar(
                    evt,
                    "OutcomeType",
                    nameof(CustomFormatGroupReferenceMismatchPlanningOutcome)
                ) && PropertyContains(evt, "Outcome", "missing-group")
            )
            .Which;
        blockingPlanningOutcome.Level.Should().Be(LogEventLevel.Error);
        var nonBlockingPlanningOutcome = log
            .Events.Should()
            .ContainSingle(evt =>
                HasScalar(
                    evt,
                    "OutcomeType",
                    nameof(CustomFormatGroupDefaultItemSelectedPlanningOutcome)
                ) && PropertyContains(evt, "Outcome", "default-cf")
            )
            .Which;
        nonBlockingPlanningOutcome.Level.Should().Be(LogEventLevel.Warning);
    }

    private static SyncRunResult CreateResult()
    {
        var identity = new CustomFormatIdentity("cf-one", "First CF");
        var customFormats = new CustomFormatPipelineResult(
            completedResources: 1,
            incompleteResources: 0,
            [new CustomFormatAdoptedOutcome(identity, 10)],
            [
                new CustomFormatCreateDelta(
                    identity,
                    new CustomFormatSourceInfo(
                        CfSource.CfGroupExplicit,
                        "Group",
                        CfInclusionReason.Selected,
                        ["Profile"]
                    )
                ),
            ]
        );
        var qualityProfiles = new QualityProfilePipelineResult(
            completedResources: 0,
            incompleteResources: 1,
            [new QualityProfileReferenceMismatchOutcome("missing-profile")],
            []
        );
        var qualitySizes = new QualitySizePipelineResult(
            completedResources: 1,
            incompleteResources: 1,
            [new QualitySizePreferredRatioClampedOutcome(new ValueDelta<decimal>(1.5m, 1m))],
            [
                new QualitySizeDelta(
                    "Bluray",
                    [
                        new QualitySizeMinimumChanged(
                            new ValueDelta<QualitySizeValue>(
                                new QualitySizeValue.Numeric(1m),
                                new QualitySizeValue.Numeric(2m)
                            )
                        ),
                    ]
                ),
            ]
        );
        var sonarrNaming = new SonarrNamingPipelineResult(
            completedFields: 1,
            incompleteFields: 1,
            [
                new SonarrNamingReferenceMismatchOutcome(
                    SonarrNamingFormatField.SeriesFolderFormat,
                    "bad-series"
                ),
            ],
            new SonarrNamingDelta { SeriesFolderFormat = new ValueDelta<string?>("Old", "Series") }
        );
        var mediaManagement = new MediaManagementPipelineResult(
            SyncResultStatus.Succeeded,
            new MediaManagementDelta(
                new ValueDelta<PropersAndRepacksMode?>(null, PropersAndRepacksMode.DoNotPrefer)
            )
        );
        var radarrNaming = new RadarrNamingPipelineResult(
            completedFields: 1,
            incompleteFields: 1,
            [
                new RadarrNamingReferenceMismatchOutcome(
                    RadarrNamingFormatField.StandardMovieFormat,
                    "bad-movie"
                ),
            ],
            new RadarrNamingDelta { StandardMovieFormat = new ValueDelta<string?>("Old", "Movie") }
        );

        return new SyncRunResult(
            [
                new SyncInstanceResult(
                    "tv",
                    SupportedServices.Sonarr,
                    [customFormats, qualityProfiles, qualitySizes, sonarrNaming, mediaManagement],
                    planningOutcomes:
                    [
                        new CustomFormatGroupReferenceMismatchPlanningOutcome("missing-group"),
                        new CustomFormatGroupDefaultItemSelectedPlanningOutcome(
                            "default-group",
                            "default-cf"
                        ),
                    ]
                ),
                new SyncInstanceResult("movies", SupportedServices.Radarr, [radarrNaming]),
                new SyncInstanceResult(
                    "offline",
                    SupportedServices.Radarr,
                    [],
                    new ServiceUnavailableFailure(),
                    fault: new SyncFault("instance-fault")
                ),
            ],
            new SyncFault("fault-reference")
        );
    }

    private static object? GetScalar(LogEvent evt, string property)
    {
        return evt
            .Properties.Should()
            .ContainKey(property)
            .WhoseValue.Should()
            .BeOfType<ScalarValue>()
            .Which.Value;
    }

    private static bool HasScalar(LogEvent evt, string property, object expected)
    {
        return evt.Properties.TryGetValue(property, out var value)
            && value is ScalarValue scalar
            && Equals(scalar.Value, expected);
    }

    private static bool PropertyContains(LogEvent evt, string property, object expected)
    {
        return evt.Properties.TryGetValue(property, out var value)
            && ContainsScalar(value, expected);
    }

    private static bool ContainsScalar(LogEventPropertyValue value, object expected) =>
        value switch
        {
            ScalarValue scalar => Equals(scalar.Value, expected),
            SequenceValue sequence => sequence.Elements.Any(x => ContainsScalar(x, expected)),
            StructureValue structure => structure.Properties.Any(x =>
                ContainsScalar(x.Value, expected)
            ),
            DictionaryValue dictionary => dictionary.Elements.Any(x =>
                ContainsScalar(x.Key, expected) || ContainsScalar(x.Value, expected)
            ),
            _ => false,
        };

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
