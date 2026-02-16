using System.Globalization;
using System.Reactive.Disposables;
using System.Text;
using Autofac.Features.Indexed;
using Flurl.Http;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Notifications;

public sealed class NotificationService : IDisposable
{
    private const string NoInstance = "[no instance]";

    private readonly ILogger _log;
    private readonly IIndex<AppriseMode, IAppriseNotificationApiService> _apiFactory;
    private readonly VerbosityOptions _verbosity;
    private readonly AppriseNotificationSettings? _settings;

    private readonly List<InstanceEvent> _instances = [];
    private readonly List<PipelineEvent> _pipelines = [];
    private readonly List<SyncDiagnosticEvent> _diagnostics = [];
    private readonly CompositeDisposable _subscriptions;

    public NotificationService(
        ILogger log,
        IIndex<AppriseMode, IAppriseNotificationApiService> apiFactory,
        ISettings<NotificationSettings> settings,
        ISyncRunScope run,
        VerbosityOptions verbosity
    )
    {
        _log = log;
        _apiFactory = apiFactory;
        _verbosity = verbosity;
        _settings = settings.Value.Apprise;

        _subscriptions =
        [
            run.Instances.Subscribe(_instances.Add),
            run.Pipelines.Subscribe(_pipelines.Add),
            run.Diagnostics.Subscribe(_diagnostics.Add),
        ];
    }

    public async Task SendNotification()
    {
        if (_settings is null)
        {
            _log.Debug(
                "Notification settings are not present, so this notification will not be sent"
            );
            return;
        }

        var succeeded = _instances.All(i => i.Status != InstanceProgressStatus.Failed);
        var messageType = succeeded ? AppriseMessageType.Success : AppriseMessageType.Failure;
        var body = BuildNotificationBody();
        await SendAppriseNotification(succeeded, body, messageType);
    }

    private async Task SendAppriseNotification(
        bool succeeded,
        string body,
        AppriseMessageType messageType
    )
    {
        if (string.IsNullOrEmpty(body) && !_verbosity.SendEmpty)
        {
            _log.Debug("Skipping notification because the body is empty");
            return;
        }

        // Apprise doesn't like empty bodies, so the hyphens are there in case there are no
        // notifications to render. This also creates separation between the title and the content.
        body = "---\n" + body.Trim();

        try
        {
            var api = _apiFactory[_settings!.Mode!.Value];

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
            _log.Error(e, "Failed to send notification");
        }
    }

    private string BuildNotificationBody()
    {
        var body = new StringBuilder();

        // Handle diagnostics without an instance (general errors)
        var generalDiagnostics = _diagnostics.Where(e => e.Instance is null).ToList();

        if (generalDiagnostics.Count > 0)
        {
            RenderSection(body, NoInstance, null, generalDiagnostics);
        }

        // Build per-instance pipeline results from flat event lists
        var instanceNames = _instances.Select(i => i.Name).Distinct().OrderBy(n => n);

        foreach (var instanceName in instanceNames)
        {
            var instanceDiagnostics = _diagnostics
                .Where(e =>
                    e.Instance?.Equals(instanceName, StringComparison.OrdinalIgnoreCase) == true
                )
                .ToList();

            // Build pipeline snapshot dict from pipeline events for this instance
            var pipelineSnapshots = _pipelines
                .Where(p => p.Instance.Equals(instanceName, StringComparison.OrdinalIgnoreCase))
                .GroupBy(p => p.Type)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var last = g.Last();
                        return new PipelineSnapshot(last.Status, last.Count);
                    }
                );

            RenderSection(body, instanceName, pipelineSnapshots, instanceDiagnostics);
        }

        return body.ToString();
    }

    private void RenderSection(
        StringBuilder body,
        string instanceName,
        Dictionary<PipelineType, PipelineSnapshot>? pipelineSnapshots,
        IReadOnlyList<SyncDiagnosticEvent> diagnostics
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

        // Render completion counts from pipeline events (Information category)
        if (pipelineSnapshots is not null && _verbosity.SendInfo)
        {
            var pipelineResults = Enum.GetValues<PipelineType>()
                .Select(pt =>
                    (
                        Type: pt,
                        Result: pipelineSnapshots.TryGetValue(pt, out var r)
                            ? r
                            : (PipelineSnapshot?)null
                    )
                )
                .Where(x =>
                    x.Result is { Status: PipelineProgressStatus.Succeeded, Count: not null }
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
        var errors = diagnostics.Where(e => e.Level == SyncDiagnosticLevel.Error).ToList();
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
            .Where(e => e.Level is SyncDiagnosticLevel.Warning or SyncDiagnosticLevel.Deprecation)
            .ToList();
        if (warnings.Count > 0)
        {
            body.AppendLine("Warnings:");
            body.AppendLine();
            foreach (var evt in warnings)
            {
                var prefix = evt.Level == SyncDiagnosticLevel.Deprecation ? "[DEPRECATED] " : "";
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

    public void Dispose() => _subscriptions.Dispose();
}
