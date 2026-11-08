using System.Globalization;
using System.Text;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.MediaManagement;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Pipelines.QualitySize;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Server.Sync.Notifications.Apprise;
using Recyclarr.Server.Sync.Notifications.Apprise.Dto;
using Recyclarr.Server.Sync.Results;
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

        if (instance.Fault is not null)
        {
            reports.Add(
                new NotificationReport(
                    ReportLevel.Error,
                    $"Unexpected sync fault. Reference: `{instance.Fault.Reference}`"
                )
            );
        }

        reports.AddRange(
            instance.PlanningOutcomes.Select(x => ToReport(OutcomeResponseMapper.Map(x)))
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
                x.Resources.Where(y => y.Action == PlannedAction.Create)
                    .Select(y => y.Identity.Name),
                x.Resources.Where(y => y.Action == PlannedAction.Update)
                    .Select(y => y.Identity.Name),
                x.Deletes.Select(y => y.Name)
            ),
            QualityProfilePipelineResult x => new ResourceChanges(
                x.Resources.Where(y => y.Action == PlannedAction.Create)
                    .Select(y => GetQualityProfileName(y.Identity)),
                x.Resources.Where(y => y.Action == PlannedAction.Update)
                    .Select(y => GetQualityProfileName(y.Identity)),
                []
            ),
            QualitySizePipelineResult x => new ResourceChanges(
                [],
                x.Resources.Where(y => y.Action == PlannedAction.Update).Select(y => y.Quality),
                []
            ),
            SonarrNamingPipelineResult { Settings.Action: PlannedAction.Update } =>
                new ResourceChanges([], ["Naming settings"], []),
            RadarrNamingPipelineResult { Settings.Action: PlannedAction.Update } =>
                new ResourceChanges([], ["Naming settings"], []),
            MediaManagementPipelineResult { Settings.Action: PlannedAction.Update } =>
                new ResourceChanges([], ["Media management settings"], []),
            SonarrNamingPipelineResult
            or RadarrNamingPipelineResult
            or MediaManagementPipelineResult => new ResourceChanges([], [], []),
            _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
        };

    private static IEnumerable<NotificationReport> FormatPipelineOutcomes(
        PipelineResult pipeline
    ) =>
        (
            pipeline switch
            {
                CustomFormatPipelineResult x => x.Outcomes.Select(OutcomeResponseMapper.Map),
                QualityProfilePipelineResult x => x.Outcomes.Select(OutcomeResponseMapper.Map),
                QualitySizePipelineResult x => x.Outcomes.Select(y => OutcomeResponseMapper.Map(y)),
                SonarrNamingPipelineResult x => x.Outcomes.Select(OutcomeResponseMapper.Map),
                RadarrNamingPipelineResult x => x.Outcomes.Select(OutcomeResponseMapper.Map),
                MediaManagementPipelineResult => [],
                _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
            }
        ).Select(ToReport);

    private static NotificationReport ToReport(OutcomeResponse outcome) =>
        new(
            outcome.Severity switch
            {
                OutcomeSeverity.Error => ReportLevel.Error,
                OutcomeSeverity.Warning => ReportLevel.Warning,
                OutcomeSeverity.Info => ReportLevel.Information,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
            },
            outcome.Message
        );

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

    private static string FormatFailure(OperationalFailure failure) =>
        failure switch
        {
            ServiceUnavailableFailure => "The service is unavailable",
            ServiceUnauthenticatedFailure => "The service rejected the API credentials",
            ServiceUnauthorizedFailure => "The service denied access",
            ServiceRateLimitedFailure => "The service rate limit was exceeded",
            ServiceRedirectedFailure =>
                "The service redirected the request; check base_url and any reverse proxy",
            ServiceIncompatibleFailure =>
                "The connected service is incompatible with this configuration",
            SyncStateUnavailableFailure => "The persisted synchronization state is unavailable",
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
        };

    private static string GetQualityProfileName(QualityProfileIdentity identity) =>
        identity switch
        {
            GuideBackedQualityProfileIdentity x => $"{x.MappingKey.Name} ({x.MappingKey.TrashId})",
            UserDefinedQualityProfileIdentity x => x.Name,
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, null),
        };

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
