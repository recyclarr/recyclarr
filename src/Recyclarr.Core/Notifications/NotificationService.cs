using System.Globalization;
using System.Text;
using Autofac.Features.Indexed;
using Flurl.Http;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Events;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Notifications;

public sealed class NotificationService(
    ILogger log,
    IIndex<AppriseMode, IAppriseNotificationApiService> apiFactory,
    ISettings<NotificationSettings> settings,
    SyncEventStorage eventStorage,
    VerbosityOptions verbosity
)
{
    private const string NoInstance = "[no instance]";
    private readonly AppriseNotificationSettings? _settings = settings.Value.Apprise;

    public async Task SendNotification(bool succeeded, ProgressSnapshot snapshot)
    {
        if (_settings is null)
        {
            log.Debug(
                "Notification settings are not present, so this notification will not be sent"
            );
            return;
        }

        var messageType = succeeded ? AppriseMessageType.Success : AppriseMessageType.Failure;
        var body = BuildNotificationBody(snapshot);
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

        // Apprise doesn't like empty bodies, so the hyphens are there in case there are no notifications to render.
        // This also doesn't look too bad because it creates some separation between the title and the content.
        body = "---\n" + body.Trim();

        try
        {
            var api = apiFactory[_settings!.Mode!.Value];

            await api.Notify(
                _settings!,
                payload =>
                    payload with
                    {
                        Title = $"Recyclarr Sync {(succeeded ? "Completed" : "Failed")}",
                        Body = body,
                        Type = messageType,
                        Format = AppriseMessageFormat.Markdown,
                    }
            );
        }
        catch (FlurlHttpException e)
        {
            log.Error(e, "Failed to send notification");
        }
    }

    private string BuildNotificationBody(ProgressSnapshot snapshot)
    {
        var body = new StringBuilder();

        // Handle diagnostics without an instance (general errors)
        var generalDiagnostics = eventStorage
            .Diagnostics.Where(e => e.InstanceName is null)
            .ToList();

        if (generalDiagnostics.Count > 0)
        {
            RenderSection(body, NoInstance, null, generalDiagnostics);
        }

        // Render each instance from progress snapshot
        foreach (var instance in snapshot.Instances.OrderBy(i => i.Name))
        {
            var instanceDiagnostics = eventStorage
                .Diagnostics.Where(e =>
                    e.InstanceName?.Equals(instance.Name, StringComparison.OrdinalIgnoreCase)
                    == true
                )
                .ToList();

            RenderSection(body, instance.Name, instance, instanceDiagnostics);
        }

        return body.ToString();
    }

    private void RenderSection(
        StringBuilder body,
        string instanceName,
        InstanceSnapshot? progressInstance,
        IReadOnlyList<DiagnosticEvent> diagnostics
    )
    {
        if (instanceName == NoInstance)
        {
            body.AppendLine("### General");
        }
        else
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"### Instance: `{instanceName}`");
        }

        body.AppendLine();

        // Render completion counts from progress snapshot (Information category)
        if (progressInstance is not null && verbosity.SendInfo)
        {
            var pipelineResults = Enum.GetValues<PipelineType>()
                .Select(pt =>
                    (
                        Type: pt,
                        Result: progressInstance.Value.Pipelines.TryGetValue(pt, out var r)
                            ? r
                            : (PipelineSnapshot?)null
                    )
                )
                .Where(x =>
                    x.Result is not null
                    && x.Result.Value.Status == PipelineProgressStatus.Succeeded
                    && x.Result.Value.Count.HasValue
                )
                .ToList();

            if (pipelineResults.Count > 0)
            {
                body.AppendLine("Information:");
                body.AppendLine();
                foreach (var (pipelineType, result) in pipelineResults)
                {
                    var description = GetPipelineDescription(pipelineType);
                    body.AppendLine(
                        CultureInfo.InvariantCulture,
                        $"- {description}: {result!.Value.Count}"
                    );
                }

                body.AppendLine();
            }
        }

        // Render errors
        var errors = diagnostics.Where(e => e.Type == DiagnosticType.Error).ToList();
        if (errors.Count > 0)
        {
            body.AppendLine("Errors:");
            body.AppendLine();
            foreach (var evt in errors)
            {
                body.AppendLine(CultureInfo.InvariantCulture, $"- {evt.Message}");
            }

            body.AppendLine();
        }

        // Render warnings (including deprecations)
        var warnings = diagnostics
            .Where(e => e.Type is DiagnosticType.Warning or DiagnosticType.Deprecation)
            .ToList();
        if (warnings.Count > 0)
        {
            body.AppendLine("Warnings:");
            body.AppendLine();
            foreach (var evt in warnings)
            {
                var prefix = evt.Type == DiagnosticType.Deprecation ? "[DEPRECATED] " : "";
                body.AppendLine(CultureInfo.InvariantCulture, $"- {prefix}{evt.Message}");
            }

            body.AppendLine();
        }
    }

    private static string GetPipelineDescription(PipelineType pipeline)
    {
        return pipeline switch
        {
            PipelineType.CustomFormat => "Custom Formats Synced",
            PipelineType.QualityProfile => "Quality Profiles Synced",
            PipelineType.QualitySize => "Quality Sizes Synced",
            PipelineType.MediaNaming => "Media Naming Synced",
            _ => "Items Synced",
        };
    }
}
