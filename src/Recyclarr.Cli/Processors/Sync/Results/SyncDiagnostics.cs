using System.Globalization;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync.Results;

internal enum DiagnosticSeverity
{
    Error,
    Warning,
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
/// Turns terminal sync results into user-facing diagnostics. The wording follows the server's
/// notification text so both channels describe an outcome the same way; the CLI keeps its own copy
/// because it reads response DTOs and must not reference Recyclarr.Core.
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
            yield return Planning(outcome);
        }

        PipelineCheck?[] pipelines =
        [
            instance.CustomFormats is { } cf
                ? new(PipelineNames.CustomFormats, cf.Status, cf.BlockedBy, [.. CustomFormats(cf)])
                : null,
            instance.QualityProfiles is { } qp
                ? new(
                    PipelineNames.QualityProfiles,
                    qp.Status,
                    qp.BlockedBy,
                    [.. QualityProfiles(qp)]
                )
                : null,
            instance.QualitySizes is { } qs
                ? new(PipelineNames.QualitySizes, qs.Status, qs.BlockedBy, [.. QualitySizes(qs)])
                : null,
            instance.Naming is { } naming
                ? new(
                    PipelineNames.MediaNaming,
                    naming.Status,
                    naming.BlockedBy,
                    [.. Naming(naming)]
                )
                : null,
            instance.MediaManagement is { } mm
                ? new(PipelineNames.MediaManagement, mm.Status, mm.BlockedBy, [])
                : null,
        ];

        foreach (var pipeline in pipelines.OfType<PipelineCheck>())
        {
            foreach (var outcome in pipeline.Outcomes)
            {
                yield return outcome;
            }

            if (pipeline.Status == PipelineStatus.Succeeded || pipeline.Outcomes.Count > 0)
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

    private static (DiagnosticSeverity, string) Planning(PlanningOutcomeResponse outcome) =>
        outcome switch
        {
            PlanningOutcomeResponseCustomFormatGroupReferenceMismatch x => Error(
                $"Custom Format group '{x.GroupTrashId}' does not exist"
            ),
            PlanningOutcomeResponseCustomFormatGroupSelectReferenceMismatch x => Error(
                $"Custom Format group '{x.GroupTrashId}' does not contain selected format "
                    + $"'{x.CustomFormatTrashId}'"
            ),
            PlanningOutcomeResponseCustomFormatGroupExcludeReferenceMismatch x => Error(
                $"Custom Format group '{x.GroupTrashId}' does not contain excluded format "
                    + $"'{x.CustomFormatTrashId}'"
            ),
            PlanningOutcomeResponseCustomFormatGroupQualityProfileReferenceMismatch x => Error(
                $"Custom Format group '{x.GroupTrashId}' references unknown quality profile "
                    + $"'{x.ProfileTrashId}'"
            ),
            PlanningOutcomeResponseCustomFormatQualityProfileReferenceAmbiguous x => Error(
                $"Quality profile '{x.ProfileTrashId}' matches multiple configured profiles: "
                    + Names(x.ProfileNames)
            ),
            PlanningOutcomeResponseCustomFormatGroupQualityProfileReferenceAmbiguous x => Error(
                $"Custom Format group '{x.GroupTrashId}' references quality profile "
                    + $"'{x.ProfileTrashId}', which matches multiple configured profiles: "
                    + Names(x.ProfileNames)
            ),
            PlanningOutcomeResponseCustomFormatGroupRequiredItemSelected x => Warning(
                $"Custom Format '{x.CustomFormatTrashId}' is required by group "
                    + $"'{x.GroupTrashId}' and does not need to be selected"
            ),
            PlanningOutcomeResponseCustomFormatGroupDefaultItemSelected x => Warning(
                $"Custom Format '{x.CustomFormatTrashId}' is included by default in group "
                    + $"'{x.GroupTrashId}' and does not need to be selected"
            ),
            PlanningOutcomeResponseCustomFormatGroupRequiredItemExcluded x => Warning(
                $"Custom Format '{x.CustomFormatTrashId}' is required by group "
                    + $"'{x.GroupTrashId}' and cannot be excluded"
            ),
            PlanningOutcomeResponseCustomFormatGroupNonDefaultItemExcluded x => Warning(
                $"Excluding Custom Format '{x.CustomFormatTrashId}' from group "
                    + $"'{x.GroupTrashId}' has no effect unless all optional formats are "
                    + "selected"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static IEnumerable<(DiagnosticSeverity, string)> CustomFormats(
        CustomFormatPipelineResponse pipeline
    )
    {
        var o = pipeline.Outcomes;
        return
        [
            .. (o.ReferenceMismatches ?? []).Select(x =>
                Error($"Custom Format '{x}' does not exist")
            ),
            .. (o.GroupReferenceMismatches ?? []).Select(x =>
                Error($"Custom Format group '{x}' does not exist")
            ),
            .. (o.IncompatibleGroups ?? []).Select(x =>
                Warning(
                    $"Custom Format group {Identity(x)} is not compatible with any selected "
                        + "quality profile"
                )
            ),
            .. (o.EmptyGroups ?? []).Select(x =>
                Warning($"Custom Format group {Identity(x)} contains no selected formats")
            ),
            .. (o.Adopted ?? []).Select(x =>
                Warning(
                    $"Adopted Custom Format {Identity(x.Identity)} from service ID {x.ServiceId}"
                )
            ),
            .. (o.AmbiguousMatches ?? []).Select(x =>
                Error(
                    $"Custom Format {Identity(x.Identity)} matches multiple service formats: "
                        + Matches(x.ServiceMatches)
                )
            ),
            .. (o.StateConflicts ?? []).Select(x =>
                Error(
                    $"Custom Format {Identity(x.Identity)} conflicts with managed format "
                        + $"{Identity(x.ManagedIdentity)} at service ID {x.ServiceId}"
                )
            ),
            .. (o.CreateRejected ?? []).Select(x =>
                Error($"The service rejected creation of Custom Format {Identity(x)}")
            ),
            .. (o.UpdateRejected ?? []).Select(x =>
                Error($"The service rejected update of Custom Format {Identity(x)}")
            ),
            .. (o.DeleteRejected ?? []).Select(x =>
                Error($"The service rejected deletion of Custom Format {Identity(x)}")
            ),
        ];
    }

    private static IEnumerable<(DiagnosticSeverity, string)> QualityProfiles(
        QualityProfileResults pipeline
    )
    {
        var o = pipeline.Outcomes;
        return
        [
            .. (o.ReferenceMismatches ?? []).Select(x =>
                Error($"Quality profile '{x}' does not exist")
            ),
            .. (o.DuplicateNames ?? []).Select(x =>
                Error($"Quality profile name '{x.Name}' is configured more than once")
            ),
            .. (o.ScoreCollisions ?? []).Select(x =>
                Error(
                    $"Custom Formats '{x.Existing.Name}' ({x.Existing.TrashId}) and "
                        + $"'{x.Rejected.Name}' ({x.Rejected.TrashId}) both resolve to service "
                        + $"ID {x.ServiceId}"
                )
            ),
            .. (o.NotFound ?? []).Select(x =>
                Error($"Quality profile {Profile(x.Identity)} was not found")
            ),
            .. (o.Adopted ?? []).Select(x =>
                Warning(
                    $"Adopted quality profile {Profile(x.Identity)} from service ID {x.ServiceId}"
                )
            ),
            .. (o.MinimumScoresUnsatisfied ?? []).Select(x =>
                Error(
                    $"Quality profile {Profile(x.Identity)} cannot satisfy minimum score "
                        + $"{x.MinimumScore}; positive score {x.TotalPositiveScore}, maximum "
                        + $"{x.MaximumScore}"
                )
            ),
            .. (o.InvalidCutoffs ?? []).Select(x =>
                Error($"Quality profile {Profile(x.Identity)} has invalid cutoff '{x.QualityName}'")
            ),
            .. (o.UnavailableCutoffs ?? []).Select(x =>
                Error(
                    $"Quality profile {Profile(x.Identity)} cannot use unavailable cutoff "
                        + $"'{x.QualityName}'"
                )
            ),
            .. (o.QualitiesRequired ?? []).Select(x =>
                Error($"Quality profile {Profile(x.Identity)} must contain a quality")
            ),
            .. (o.QualityReferenceMismatches ?? []).Select(x =>
                Error(
                    $"Quality profile {Profile(x.Identity)} references unknown qualities: "
                        + Names(x.Names)
                )
            ),
            .. (o.ResetScoreReferenceMismatches ?? []).Select(x =>
                Error(
                    $"Quality profile {Profile(x.Identity)} cannot reset scores for unknown "
                        + $"Custom Formats ({Names(x.Names)}) or unmatched patterns "
                        + $"({Names(x.Patterns)})"
                )
            ),
            .. (o.RenameBlocked ?? []).Select(x =>
                Error(
                    $"Quality profile {Profile(x.Identity)} cannot be renamed because "
                        + $"'{x.Conflict.Name}' already exists at service ID "
                        + $"{x.Conflict.ServiceId}"
                )
            ),
            .. (o.AmbiguousMatches ?? []).Select(x =>
                Error(
                    $"Quality profile {Profile(x.Identity)} matches multiple service profiles: "
                        + Matches(x.ServiceMatches)
                )
            ),
            .. (o.CreateRejected ?? []).Select(x =>
                Error($"The service rejected creation of quality profile {Profile(x.Identity)}")
            ),
            .. (o.UpdateRejected ?? []).Select(x =>
                Error($"The service rejected update of quality profile {Profile(x.Identity)}")
            ),
        ];
    }

    private static IEnumerable<(DiagnosticSeverity, string)> QualitySizes(
        QualitySizePipelineResponse pipeline
    )
    {
        var o = pipeline.Outcomes;
        return
        [
            .. (o.DefinitionReferenceMismatches ?? []).Select(x =>
                Error($"Quality definition type '{x.Type}' does not exist")
            ),
            .. (o.ReferenceMismatches ?? []).Select(x =>
                Error($"Quality '{x.Quality}' does not exist in quality definition '{x.Type}'")
            ),
            .. (o.ServiceQualitiesNotFound ?? []).Select(x =>
                Warning($"The service does not contain quality '{x}'")
            ),
            .. (o.PreferredRatiosClamped ?? []).Select(x =>
                Warning($"Preferred ratio {Number(x.Current)} was clamped to {Number(x.Desired)}")
            ),
            .. (o.MinimumGreaterThanPreferred ?? []).Select(x =>
                Error(
                    $"Quality '{x.Quality}' minimum {QualitySizeText.Of(x.Minimum)} exceeds "
                        + $"preferred {QualitySizeText.Of(x.Preferred)}"
                )
            ),
            .. (o.UnlimitedPreferredGreaterThanMaximum ?? []).Select(x =>
                Error(
                    $"Quality '{x.Quality}' preferred value is unlimited and exceeds maximum "
                        + QualitySizeText.Of(x.Maximum)
                )
            ),
            .. (o.PreferredGreaterThanMaximum ?? []).Select(x =>
                Error(
                    $"Quality '{x.Quality}' preferred {QualitySizeText.Of(x.Preferred)} exceeds "
                        + $"maximum {QualitySizeText.Of(x.Maximum)}"
                )
            ),
        ];
    }

    private static IEnumerable<(DiagnosticSeverity, string)> Naming(NamingResults naming) =>
        naming.Mismatches.Select(x =>
            Error($"Media naming field '{x.Field}' references unknown format '{x.ConfiguredKey}'")
        );

    private static string Identity(TrashIdNameResponse identity) =>
        $"'{identity.Name}' ({identity.TrashId})";

    private static string Profile(QualityProfileIdentityResponse identity) =>
        identity.TrashId is { } trashId ? $"'{identity.Name} ({trashId})'" : $"'{identity.Name}'";

    private static string Names(IEnumerable<string> names) =>
        string.Join(", ", names.Select(x => $"'{x}'"));

    private static string Matches(IEnumerable<NamedServiceResourceResponse> matches) =>
        string.Join(", ", matches.Select(x => $"'{x.Name}' (ID {x.ServiceId})"));

    private static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static (DiagnosticSeverity, string) Error(string message) =>
        (DiagnosticSeverity.Error, message);

    private static (DiagnosticSeverity, string) Warning(string message) =>
        (DiagnosticSeverity.Warning, message);

    private sealed record PipelineCheck(
        string Name,
        PipelineStatus Status,
        BlockingPipeline? BlockedBy,
        IReadOnlyList<(DiagnosticSeverity Severity, string Message)> Outcomes
    );
}
