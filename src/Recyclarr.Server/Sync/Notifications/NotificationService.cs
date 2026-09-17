using System.Globalization;
using System.Text;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Sync.Notifications.Apprise;
using Recyclarr.Server.Sync.Notifications.Apprise.Dto;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Sync.Notifications;

internal sealed class NotificationService(
    ILogger log,
    IAppriseNotificationApiService api,
    VerbosityOptions verbosity
) : INotificationService
{
    private const int MaxItemsPerAction = 20;

    public async Task SendNotification(SyncRunResult result)
    {
        var succeeded = result.Status == SyncResultStatus.Succeeded;
        var messageType = succeeded ? AppriseMessageType.Success : AppriseMessageType.Failure;
        var body = BuildNotificationBody(result);
        await SendAppriseNotification(succeeded, body, messageType);
    }

    private async Task SendAppriseNotification(
        bool succeeded,
        string body,
        AppriseMessageType messageType
    )
    {
        if (string.IsNullOrEmpty(body) && !verbosity.SendEmpty)
        {
            log.Debug("Skipping notification because the body is empty");
            return;
        }

        // Apprise doesn't like empty bodies, so the hyphens are there in case there are no
        // notifications to render. This also creates separation between the title and the content.
        body = "---\n" + body.Trim();

        await api.Notify(payload =>
            payload with
            {
                Title = $"Recyclarr Sync {(succeeded ? "Completed" : "Failed")}",
                Body = body,
                Type = messageType,
                Format = AppriseMessageFormat.Markdown,
            }
        );
    }

    private string BuildNotificationBody(SyncRunResult result)
    {
        var body = new StringBuilder();

        if (result.Fault is not null)
        {
            body.AppendLine("### General");
            body.AppendLine();
            body.AppendLine(
                CultureInfo.InvariantCulture,
                $"- Unexpected sync fault. Reference: `{result.Fault.Reference}`"
            );
            body.AppendLine();
        }

        foreach (
            var instance in result.Instances.OrderBy(
                x => x.InstanceName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            RenderInstance(body, instance);
        }

        return body.ToString();
    }

    private void RenderInstance(StringBuilder body, SyncInstanceResult instance)
    {
        var section = new StringBuilder();
        var reports = new List<NotificationReport>();

        if (instance.Failure is not null)
        {
            reports.Add(new NotificationReport(ReportLevel.Error, FormatFailure(instance.Failure)));
        }

        reports.AddRange(
            instance.PlanningOutcomes.Select(outcome => new NotificationReport(
                outcome is BlockingPlanningOutcome ? ReportLevel.Error : ReportLevel.Warning,
                FormatPlanningOutcome(outcome)
            ))
        );

        foreach (var pipeline in instance.Pipelines)
        {
            reports.AddRange(FormatPipelineOutcomes(pipeline));

            if (pipeline.Status != SyncResultStatus.Succeeded && !HasOutcomes(pipeline))
            {
                var reason = pipeline.BlockedBy is not null
                    ? $"blocked by {GetPipelineName(pipeline.BlockedBy.Value)}"
                    : $"completed with status {pipeline.Status}";
                reports.Add(
                    new NotificationReport(
                        ReportLevel.Error,
                        $"{GetPipelineName(pipeline)} {reason}"
                    )
                );
            }

            if (verbosity.SendInfo)
            {
                AppendPipelineChanges(section, pipeline);
            }
        }

        AppendReports(section, "Errors", reports, ReportLevel.Error);
        AppendReports(section, "Warnings", reports, ReportLevel.Warning);
        if (verbosity.SendInfo)
        {
            AppendReports(section, "Information", reports, ReportLevel.Information);
        }

        if (section.Length == 0)
        {
            return;
        }

        body.AppendLine(CultureInfo.InvariantCulture, $"### Instance: `{instance.InstanceName}`");
        body.AppendLine();
        body.Append(section);
    }

    private static void AppendReports(
        StringBuilder section,
        string heading,
        IEnumerable<NotificationReport> reports,
        ReportLevel level
    )
    {
        var matching = reports.Where(x => x.Level == level).ToList();
        if (matching.Count == 0)
        {
            return;
        }

        section.AppendLine(CultureInfo.InvariantCulture, $"{heading}:");
        section.AppendLine();
        foreach (var report in matching)
        {
            section.AppendLine(CultureInfo.InvariantCulture, $"- {report.Message}");
        }

        section.AppendLine();
    }

    private void AppendPipelineChanges(StringBuilder section, PipelineResult pipeline)
    {
        var changes = GetResourceChanges(pipeline);
        if (changes.Count == 0)
        {
            return;
        }

        section.AppendLine(
            CultureInfo.InvariantCulture,
            $"{GetPipelineName(pipeline)} Changed: {changes.Count}"
        );

        if (verbosity.SendItemDetails)
        {
            AppendActionItems(section, "Created", changes.Created);
            AppendActionItems(section, "Updated", changes.Updated);
            AppendActionItems(section, "Deleted", changes.Deleted);
        }

        section.AppendLine();
    }

    private static void AppendActionItems(StringBuilder section, string action, List<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var displayItems = items.Take(MaxItemsPerAction);

        var line = new StringBuilder();
        line.Append("  - ");
        line.Append(action);
        line.Append(": ");
        line.Append(string.Join(", ", displayItems));

        if (items.Count > MaxItemsPerAction)
        {
            var remaining = items.Count - MaxItemsPerAction;
            line.Append(CultureInfo.InvariantCulture, $" (and {remaining} more)");
        }

        section.AppendLine(line.ToString());
    }

    private static ResourceChanges GetResourceChanges(PipelineResult pipeline) =>
        pipeline switch
        {
            CustomFormatPipelineResult x => new ResourceChanges(
                x.Deltas.OfType<CustomFormatCreateDelta>().Select(delta => delta.Identity.Name),
                x.Deltas.OfType<CustomFormatUpdateDelta>().Select(delta => delta.Identity.Name),
                x.Deltas.OfType<CustomFormatDeleteDelta>().Select(delta => delta.Identity.Name)
            ),
            QualityProfilePipelineResult x => new ResourceChanges(
                x.Deltas.OfType<QualityProfileCreateDelta>().Select(GetQualityProfileName),
                x.Deltas.OfType<QualityProfileUpdateDelta>().Select(GetQualityProfileName),
                []
            ),
            QualitySizePipelineResult x => new ResourceChanges(
                [],
                x.Deltas.Select(delta => delta.Quality),
                []
            ),
            SonarrNamingPipelineResult { Delta: not null } => new ResourceChanges(
                [],
                ["Naming settings"],
                []
            ),
            RadarrNamingPipelineResult { Delta: not null } => new ResourceChanges(
                [],
                ["Naming settings"],
                []
            ),
            MediaManagementPipelineResult { Delta: not null } => new ResourceChanges(
                [],
                ["Media management settings"],
                []
            ),
            SonarrNamingPipelineResult
            or RadarrNamingPipelineResult
            or MediaManagementPipelineResult => new ResourceChanges([], [], []),
            _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
        };

    private static IEnumerable<NotificationReport> FormatPipelineOutcomes(
        PipelineResult pipeline
    ) =>
        pipeline switch
        {
            CustomFormatPipelineResult x => x.Outcomes.Select(FormatCustomFormatOutcome),
            QualityProfilePipelineResult x => x.Outcomes.Select(FormatQualityProfileOutcome),
            QualitySizePipelineResult x => x.Outcomes.Select(FormatQualitySizeOutcome),
            SonarrNamingPipelineResult x => x.Outcomes.Select(FormatSonarrNamingOutcome),
            RadarrNamingPipelineResult x => x.Outcomes.Select(FormatRadarrNamingOutcome),
            MediaManagementPipelineResult => [],
            _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
        };

    private static bool HasOutcomes(PipelineResult pipeline) =>
        pipeline switch
        {
            CustomFormatPipelineResult x => x.Outcomes.Count > 0,
            QualityProfilePipelineResult x => x.Outcomes.Count > 0,
            QualitySizePipelineResult x => x.Outcomes.Count > 0,
            SonarrNamingPipelineResult x => x.Outcomes.Count > 0,
            RadarrNamingPipelineResult x => x.Outcomes.Count > 0,
            MediaManagementPipelineResult => false,
            _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
        };

    private static string GetPipelineName(PipelineResult pipeline) =>
        pipeline switch
        {
            CustomFormatPipelineResult => "Custom Formats",
            QualityProfilePipelineResult => "Quality Profiles",
            QualitySizePipelineResult => "Quality Sizes",
            SonarrNamingPipelineResult or RadarrNamingPipelineResult => "Media Naming",
            MediaManagementPipelineResult => "Media Management",
            _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
        };

    private static string GetPipelineName(PipelineType pipeline) =>
        pipeline switch
        {
            PipelineType.CustomFormat => "Custom Formats",
            PipelineType.QualityProfile => "Quality Profiles",
            PipelineType.QualitySize => "Quality Sizes",
            PipelineType.MediaNaming => "Media Naming",
            PipelineType.MediaManagement => "Media Management",
            _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
        };

    private static string FormatPlanningOutcome(PlanningOutcome outcome) =>
        outcome switch
        {
            CustomFormatGroupReferenceMismatchPlanningOutcome x =>
                $"Custom Format group '{x.GroupTrashId}' does not exist",
            CustomFormatGroupSelectReferenceMismatchPlanningOutcome x =>
                $"Custom Format group '{x.GroupTrashId}' does not contain selected format "
                    + $"'{x.CustomFormatTrashId}'",
            CustomFormatGroupExcludeReferenceMismatchPlanningOutcome x =>
                $"Custom Format group '{x.GroupTrashId}' does not contain excluded format "
                    + $"'{x.CustomFormatTrashId}'",
            CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcome x =>
                $"Custom Format group '{x.GroupTrashId}' references unknown quality profile "
                    + $"'{x.ProfileTrashId}'",
            CustomFormatQualityProfileReferenceAmbiguousPlanningOutcome x =>
                $"Quality profile '{x.ProfileTrashId}' matches multiple configured profiles: "
                    + FormatNames(x.ProfileNames),
            CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcome x =>
                $"Custom Format group '{x.GroupTrashId}' references quality profile "
                    + $"'{x.ProfileTrashId}', which matches multiple configured profiles: "
                    + FormatNames(x.ProfileNames),
            CustomFormatGroupRequiredItemSelectedPlanningOutcome x =>
                $"Custom Format '{x.CustomFormatTrashId}' is required by group "
                    + $"'{x.GroupTrashId}' and does not need to be selected",
            CustomFormatGroupDefaultItemSelectedPlanningOutcome x =>
                $"Custom Format '{x.CustomFormatTrashId}' is included by default in group "
                    + $"'{x.GroupTrashId}' and does not need to be selected",
            CustomFormatGroupRequiredItemExcludedPlanningOutcome x =>
                $"Custom Format '{x.CustomFormatTrashId}' is required by group "
                    + $"'{x.GroupTrashId}' and cannot be excluded",
            CustomFormatGroupNonDefaultItemExcludedPlanningOutcome x =>
                $"Excluding Custom Format '{x.CustomFormatTrashId}' from group "
                    + $"'{x.GroupTrashId}' has no effect unless all optional formats are selected",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static string FormatFailure(OperationalFailure failure) =>
        failure switch
        {
            ServiceUnavailableFailure => "The service is unavailable",
            ServiceUnauthenticatedFailure => "The service rejected the API credentials",
            ServiceUnauthorizedFailure => "The service denied access",
            ServiceRateLimitedFailure => "The service rate limit was exceeded",
            ServiceIncompatibleFailure =>
                "The connected service is incompatible with this configuration",
            SyncStateUnavailableFailure => "The persisted synchronization state is unavailable",
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
        };

    private static NotificationReport FormatCustomFormatOutcome(CustomFormatOutcome outcome) =>
        outcome switch
        {
            CustomFormatReferenceMismatchOutcome x => Error(
                $"Custom Format '{x.TrashId}' does not exist"
            ),
            CustomFormatGroupReferenceMismatchOutcome x => Error(
                $"Custom Format group '{x.TrashId}' does not exist"
            ),
            IncompatibleCustomFormatGroupOutcome x => Warning(
                $"Custom Format group '{x.Name}' ({x.TrashId}) is not compatible with any "
                    + "selected quality profile"
            ),
            EmptyCustomFormatGroupOutcome x => Warning(
                $"Custom Format group '{x.Name}' ({x.TrashId}) contains no selected formats"
            ),
            CustomFormatAdoptedOutcome x => Information(
                $"Adopted Custom Format '{x.Identity.Name}' ({x.Identity.TrashId}) from "
                    + $"service ID {x.ServiceId}"
            ),
            CustomFormatAmbiguousMatchOutcome x => Error(
                $"Custom Format '{x.Identity.Name}' ({x.Identity.TrashId}) matches multiple "
                    + $"service formats: {FormatMatches(x.ServiceMatches)}"
            ),
            CustomFormatStateConflictOutcome x => Error(
                $"Custom Format '{x.Identity.Name}' ({x.Identity.TrashId}) conflicts with "
                    + $"managed format '{x.ManagedIdentity.Name}' "
                    + $"({x.ManagedIdentity.TrashId}) at service ID {x.ServiceId}"
            ),
            CustomFormatCreateRejectedOutcome x => Error(
                $"The service rejected creation of Custom Format '{x.Identity.Name}' "
                    + $"({x.Identity.TrashId})"
            ),
            CustomFormatUpdateRejectedOutcome x => Error(
                $"The service rejected update of Custom Format '{x.Identity.Name}' "
                    + $"({x.Identity.TrashId})"
            ),
            CustomFormatDeleteRejectedOutcome x => Error(
                $"The service rejected deletion of Custom Format '{x.Identity.Name}' "
                    + $"({x.Identity.TrashId})"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static NotificationReport FormatQualityProfileOutcome(QualityProfileOutcome outcome) =>
        outcome switch
        {
            QualityProfileReferenceMismatchOutcome x => Error(
                $"Quality profile '{x.TrashId}' does not exist"
            ),
            QualityProfileDuplicateNameOutcome x => Error(
                $"Quality profile name '{x.Name}' is configured more than once"
            ),
            QualityProfileScoreCollisionOutcome x => Error(
                $"Custom Formats '{x.Existing.Name}' ({x.Existing.TrashId}) and "
                    + $"'{x.Rejected.Name}' ({x.Rejected.TrashId}) both resolve to service ID "
                    + $"{x.ServiceId}"
            ),
            QualityProfileNotFoundOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' was not found"
            ),
            QualityProfileAdoptedOutcome x => Information(
                $"Adopted quality profile '{GetQualityProfileName(x.Identity)}' from service ID "
                    + $"{x.ServiceId}"
            ),
            QualityProfileMinimumScoreUnsatisfiedOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' cannot satisfy minimum "
                    + $"score {x.MinimumScore}; positive score {x.TotalPositiveScore}, maximum "
                    + $"{x.MaximumScore}"
            ),
            QualityProfileInvalidCutoffOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' has invalid cutoff "
                    + $"'{x.QualityName}'"
            ),
            QualityProfileUnavailableCutoffOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' cannot use unavailable "
                    + $"cutoff '{x.QualityName}'"
            ),
            QualityProfileQualitiesRequiredOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' must contain a quality"
            ),
            QualityProfileQualityReferenceMismatchOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' references unknown "
                    + $"qualities: {FormatNames(x.Names)}"
            ),
            QualityProfileResetScoreReferenceMismatchOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' cannot reset scores for "
                    + $"unknown Custom Formats ({FormatNames(x.Names)}) or unmatched patterns "
                    + $"({FormatNames(x.Patterns)})"
            ),
            QualityProfileRenameBlockedOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' cannot be renamed because "
                    + $"'{x.Conflict.Name}' already exists at service ID {x.Conflict.ServiceId}"
            ),
            QualityProfileAmbiguousMatchOutcome x => Error(
                $"Quality profile '{GetQualityProfileName(x.Identity)}' matches multiple service "
                    + $"profiles: {FormatMatches(x.ServiceMatches)}"
            ),
            QualityProfileCreateRejectedOutcome x => Error(
                $"The service rejected creation of quality profile "
                    + $"'{GetQualityProfileName(x.Identity)}'"
            ),
            QualityProfileUpdateRejectedOutcome x => Error(
                $"The service rejected update of quality profile "
                    + $"'{GetQualityProfileName(x.Identity)}'"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static NotificationReport FormatQualitySizeOutcome(QualitySizeOutcome outcome) =>
        outcome switch
        {
            QualitySizeDefinitionReferenceMismatchOutcome x => Error(
                $"Quality definition type '{x.Type}' does not exist"
            ),
            QualitySizeReferenceMismatchOutcome x => Error(
                $"Quality '{x.Quality}' does not exist in quality definition '{x.Type}'"
            ),
            QualitySizeServiceQualityNotFoundOutcome x => Warning(
                $"The service does not contain quality '{x.Quality}'"
            ),
            QualitySizePreferredRatioClampedOutcome x => Warning(
                $"Preferred ratio {x.Value.Current} was clamped to {x.Value.Desired}"
            ),
            QualitySizeMinimumGreaterThanPreferredOutcome x => Error(
                $"Quality '{x.Quality}' minimum {x.Minimum} exceeds preferred {x.Preferred}"
            ),
            QualitySizeUnlimitedPreferredGreaterThanMaximumOutcome x => Error(
                $"Quality '{x.Quality}' preferred value is unlimited and exceeds maximum "
                    + $"{x.Maximum}"
            ),
            QualitySizePreferredGreaterThanMaximumOutcome x => Error(
                $"Quality '{x.Quality}' preferred {x.Preferred} exceeds maximum {x.Maximum}"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static NotificationReport FormatSonarrNamingOutcome(SonarrNamingOutcome outcome) =>
        outcome switch
        {
            SonarrNamingReferenceMismatchOutcome x => Error(
                $"Media naming field '{x.Field}' references unknown format "
                    + $"'{x.ConfiguredKey}'"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static NotificationReport FormatRadarrNamingOutcome(RadarrNamingOutcome outcome) =>
        outcome switch
        {
            RadarrNamingReferenceMismatchOutcome x => Error(
                $"Media naming field '{x.Field}' references unknown format "
                    + $"'{x.ConfiguredKey}'"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    private static string GetQualityProfileName(QualityProfileDelta delta) =>
        GetQualityProfileName(delta.Identity);

    private static string GetQualityProfileName(QualityProfileIdentity identity) =>
        identity switch
        {
            GuideBackedQualityProfileIdentity x => $"{x.MappingKey.Name} ({x.MappingKey.TrashId})",
            UserDefinedQualityProfileIdentity x => x.Name,
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, null),
        };

    private static string FormatNames(IEnumerable<string> names) =>
        string.Join(", ", names.Select(name => $"'{name}'"));

    private static string FormatMatches(IEnumerable<CustomFormatServiceMatch> matches) =>
        string.Join(", ", matches.Select(match => $"'{match.Name}' (ID {match.ServiceId})"));

    private static string FormatMatches(IEnumerable<QualityProfileServiceMatch> matches) =>
        string.Join(", ", matches.Select(match => $"'{match.Name}' (ID {match.ServiceId})"));

    private static NotificationReport Error(string message) => new(ReportLevel.Error, message);

    private static NotificationReport Warning(string message) => new(ReportLevel.Warning, message);

    private static NotificationReport Information(string message) =>
        new(ReportLevel.Information, message);

    private sealed record NotificationReport(ReportLevel Level, string Message);

    private sealed record ResourceChanges
    {
        public ResourceChanges(
            IEnumerable<string> created,
            IEnumerable<string> updated,
            IEnumerable<string> deleted
        )
        {
            Created = created.ToList();
            Updated = updated.ToList();
            Deleted = deleted.ToList();
        }

        public List<string> Created { get; }
        public List<string> Updated { get; }
        public List<string> Deleted { get; }
        public int Count => Created.Count + Updated.Count + Deleted.Count;
    }

    private enum ReportLevel
    {
        Information,
        Warning,
        Error,
    }
}
