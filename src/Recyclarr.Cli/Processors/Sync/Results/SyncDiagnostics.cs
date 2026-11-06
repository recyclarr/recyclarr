using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync.Results;

internal enum DiagnosticSeverity
{
    Error,
    Warning,
    Info,
}

/// <summary>
/// One user-actionable message from a finished sync. <see cref="Instance"/> is null when the
/// message belongs to the whole run rather than one instance.
/// </summary>
internal sealed record SyncDiagnostic(
    string? Instance,
    DiagnosticSeverity Severity,
    string Message
);

/// <summary>
/// Turns terminal sync results into user-facing diagnostics. Outcome messages come from the
/// server, so an outcome code this CLI does not know still renders.
/// </summary>
internal static class SyncDiagnostics
{
    public static IReadOnlyList<SyncDiagnostic> Build(
        SyncFaultResponse? runFault,
        IEnumerable<InstanceResults> instances
    )
    {
        var diagnostics = new List<SyncDiagnostic>();

        if (runFault is not null)
        {
            diagnostics.Add(new SyncDiagnostic(null, DiagnosticSeverity.Error, Fault(runFault)));
        }

        foreach (var instance in instances)
        {
            diagnostics.AddRange(
                ForInstance(instance)
                    .Select(x => new SyncDiagnostic(instance.Name, x.Severity, x.Message))
            );
        }

        return diagnostics;
    }

    private static IEnumerable<(DiagnosticSeverity Severity, string Message)> ForInstance(
        InstanceResults instance
    )
    {
        if (instance.Failure is not null)
        {
            yield return Error(ServiceFailureText.Describe(instance.Failure));
        }

        if (instance.Fault is not null)
        {
            yield return Error(Fault(instance.Fault));
        }

        foreach (var outcome in instance.PlanningOutcomes)
        {
            yield return FromOutcome(outcome);
        }

        PipelineCheck?[] pipelines =
        [
            instance.CustomFormats is { } cf
                ? new(PipelineNames.CustomFormats, cf.Status, cf.BlockedBy, [.. cf.Outcomes])
                : null,
            instance.QualityProfiles is { } qp
                ? new(PipelineNames.QualityProfiles, qp.Status, qp.BlockedBy, qp.Outcomes)
                : null,
            instance.QualitySizes is { } qs
                ? new(PipelineNames.QualitySizes, qs.Status, qs.BlockedBy, [.. qs.Outcomes])
                : null,
            instance.Naming is { } naming
                ? new(PipelineNames.MediaNaming, naming.Status, naming.BlockedBy, naming.Outcomes)
                : null,
            instance.MediaManagement is { } mm
                ? new(PipelineNames.MediaManagement, mm.Status, mm.BlockedBy, [])
                : null,
        ];

        foreach (var pipeline in pipelines.OfType<PipelineCheck>())
        {
            foreach (var outcome in pipeline.Outcomes)
            {
                yield return FromOutcome(outcome);
            }

            // An error outcome already explains a status other than succeeded
            if (
                pipeline.Status == PipelineStatus.Succeeded
                || pipeline.Outcomes.Any(x => x.Severity == OutcomeSeverity.Error)
            )
            {
                continue;
            }

            yield return Error(
                pipeline.BlockedBy is { } blocker
                    ? $"{pipeline.Name} blocked by {PipelineNames.Of(blocker)}"
                    : $"{pipeline.Name} completed with status {pipeline.Status}"
            );
        }
    }

    private static string Fault(SyncFaultResponse fault) =>
        $"Unexpected sync fault. Reference: {fault.Reference}";

    // The server renders the message and assigns the severity; the CLI shows both as given
    private static (DiagnosticSeverity, string) FromOutcome(OutcomeResponse outcome) =>
        (
            outcome.Severity switch
            {
                OutcomeSeverity.Error => DiagnosticSeverity.Error,
                OutcomeSeverity.Warning => DiagnosticSeverity.Warning,
                _ => DiagnosticSeverity.Info,
            },
            outcome.Message
        );

    private static (DiagnosticSeverity, string) Error(string message) =>
        (DiagnosticSeverity.Error, message);

    private sealed record PipelineCheck(
        string Name,
        PipelineStatus Status,
        BlockingPipeline? BlockedBy,
        IReadOnlyList<OutcomeResponse> Outcomes
    );
}
