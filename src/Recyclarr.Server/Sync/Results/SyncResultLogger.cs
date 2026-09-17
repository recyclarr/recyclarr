using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Sync.Results;
using Serilog.Events;

namespace Recyclarr.Server.Sync.Results;

internal sealed class SyncResultLogger(ILogger log)
{
    public void Log(JobId jobId, SyncRunResult result)
    {
        log.Write(
            GetLevel(result.Status),
            "Sync job {JobId} completed with status {Status}",
            jobId.Value,
            result.Status
        );

        if (result.Fault is not null)
        {
            log.Error(
                "Sync job {JobId} completed with fault {FaultReference}",
                jobId.Value,
                result.Fault.Reference
            );
        }

        foreach (var instance in result.Instances)
        {
            LogInstance(jobId, instance);
        }
    }

    private void LogInstance(JobId jobId, SyncInstanceResult instance)
    {
        log.Write(
            GetLevel(instance.Status),
            "Sync instance {Instance:l} ({Service}) completed with status {Status} for job {JobId}",
            instance.InstanceName,
            instance.ServiceType,
            instance.Status,
            jobId.Value
        );

        if (instance.Failure is not null)
        {
            log.Error(
                "Sync instance {Instance} stopped with {FailureType} for job {JobId}",
                instance.InstanceName,
                instance.Failure.GetType().Name,
                jobId.Value
            );
        }

        if (instance.Fault is not null)
        {
            log.Error(
                "Sync instance {Instance} completed with fault {FaultReference} for job {JobId}",
                instance.InstanceName,
                instance.Fault.Reference,
                jobId.Value
            );
        }

        foreach (var outcome in instance.PlanningOutcomes)
        {
            log.Write(
                outcome is BlockingPlanningOutcome ? LogEventLevel.Error : LogEventLevel.Warning,
                "Planning outcome for {Instance:l}: {OutcomeType:l} {@Outcome} (job {JobId})",
                instance.InstanceName,
                outcome.GetType().Name,
                outcome,
                jobId.Value
            );
        }

        foreach (var pipeline in instance.Pipelines)
        {
            LogPipeline(jobId, instance.InstanceName, pipeline);
        }
    }

    private void LogPipeline(JobId jobId, string instance, PipelineResult pipeline)
    {
        (
            string Name,
            IEnumerable<PipelineOutcome> Outcomes,
            IEnumerable<ResourceDelta> Deltas
        ) details = pipeline switch
        {
            CustomFormatPipelineResult x => ("CustomFormat", x.Outcomes, x.Deltas),
            QualityProfilePipelineResult x => ("QualityProfile", x.Outcomes, x.Deltas),
            QualitySizePipelineResult x => ("QualitySize", x.Outcomes, x.Deltas),
            SonarrNamingPipelineResult x => (
                "MediaNaming",
                x.Outcomes,
                x.Delta is null ? [] : [x.Delta]
            ),
            RadarrNamingPipelineResult x => (
                "MediaNaming",
                x.Outcomes,
                x.Delta is null ? [] : [x.Delta]
            ),
            MediaManagementPipelineResult x => (
                "MediaManagement",
                [],
                x.Delta is null ? [] : [x.Delta]
            ),
            _ => (pipeline.GetType().Name, [], []),
        };
        var level = GetLevel(pipeline.Status);
        log.Write(
            level,
            "Pipeline {Pipeline:l} for {Instance:l} completed with status {Status} "
                + "for job {JobId}",
            details.Name,
            instance,
            pipeline.Status,
            jobId.Value
        );

        foreach (var outcome in details.Outcomes)
        {
            log.Write(
                level,
                "Pipeline outcome for {Instance:l}/{Pipeline:l}: {OutcomeType:l} "
                    + "{@Outcome} (job {JobId})",
                instance,
                details.Name,
                outcome.GetType().Name,
                outcome,
                jobId.Value
            );
        }

        foreach (var delta in details.Deltas)
        {
            log.Information(
                "Pipeline change for {Instance:l}/{Pipeline:l}: {DeltaType:l} "
                    + "{@Delta} (job {JobId})",
                instance,
                details.Name,
                delta.GetType().Name,
                delta,
                jobId.Value
            );
        }
    }

    private static LogEventLevel GetLevel(SyncResultStatus status) =>
        status switch
        {
            SyncResultStatus.Succeeded => LogEventLevel.Information,
            SyncResultStatus.Partial => LogEventLevel.Warning,
            SyncResultStatus.Failed => LogEventLevel.Error,
            SyncResultStatus.Blocked => LogEventLevel.Warning,
            _ => LogEventLevel.Error,
        };
}
