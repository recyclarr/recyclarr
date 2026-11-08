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
        (string Name, IEnumerable<PipelineOutcome> Outcomes, IEnumerable<object> Changes) details =
            pipeline switch
            {
                CustomFormatPipelineResult x => (
                    "CustomFormat",
                    x.Outcomes,
                    [.. Changed(x.Resources), .. x.Deletes]
                ),
                QualityProfilePipelineResult x => (
                    "QualityProfile",
                    x.Outcomes,
                    Changed(x.Resources)
                ),
                QualitySizePipelineResult x => ("QualitySize", x.Outcomes, Changed(x.Resources)),
                SonarrNamingPipelineResult x => ("MediaNaming", x.Outcomes, Changed(x.Settings)),
                RadarrNamingPipelineResult x => ("MediaNaming", x.Outcomes, Changed(x.Settings)),
                MediaManagementPipelineResult x => ("MediaManagement", [], Changed(x.Settings)),
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

        foreach (var change in details.Changes)
        {
            log.Information(
                "Pipeline change for {Instance:l}/{Pipeline:l}: {ChangeType:l} "
                    + "{@Change} (job {JobId})",
                instance,
                details.Name,
                change.GetType().Name,
                change,
                jobId.Value
            );
        }
    }

    // Logs record what the sync changes; unchanged resources would only add noise.
    private static IEnumerable<PlannedResource> Changed(IEnumerable<PlannedResource> resources) =>
        resources.Where(x => x.Action != PlannedAction.Unchanged);

    private static IEnumerable<PlannedResource> Changed(PlannedResource? resource) =>
        resource is null ? [] : Changed([resource]);

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
