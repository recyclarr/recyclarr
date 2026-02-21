using System.Collections.Immutable;
using System.Globalization;
using System.Reactive.Disposables;
using System.Text;
using Flurl.Http;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Notifications;

internal sealed class NotificationService : INotificationService, IDisposable
{
    private const string NoInstance = "[no instance]";

    private readonly ILogger _log;
    private readonly IAppriseNotificationApiService _api;
    private readonly VerbosityOptions _verbosity;

    private readonly List<PipelineEvent> _pipelines = [];
    private readonly List<SyncDiagnosticEvent> _diagnostics = [];
    private readonly CompositeDisposable _subscriptions;

    public NotificationService(
        ILogger log,
        IAppriseNotificationApiService api,
        ISyncRunScope run,
        VerbosityOptions verbosity
    )
    {
        _log = log;
        _api = api;
        _verbosity = verbosity;

        _subscriptions =
        [
            run.Pipelines.Subscribe(_pipelines.Add),
            run.Diagnostics.Subscribe(_diagnostics.Add),
        ];
    }

    public async Task SendNotification()
    {
        // Derive overall success from pipeline statuses per instance (worst-status-wins).
        // DeriveStatus returns Running/Pending for incomplete pipelines, which would read as
        // "not failed" here. This is fine because SyncProcessor calls SendNotification() after
        // all instances finish processing, so all pipelines have terminal statuses by this point.
        var succeeded = BuildPipelineSnapshotsByInstance()
            .Values.All(snapshots =>
                InstanceSnapshot.DeriveStatus(snapshots.ToImmutableDictionary())
                != InstanceProgressStatus.Failed
            );
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
            await _api.Notify(payload =>
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
        var snapshotsByInstance = BuildPipelineSnapshotsByInstance();

        foreach (var (instanceName, pipelineSnapshots) in snapshotsByInstance.OrderBy(x => x.Key))
        {
            var instanceDiagnostics = _diagnostics
                .Where(e =>
                    e.Instance?.Equals(instanceName, StringComparison.OrdinalIgnoreCase) == true
                )
                .ToList();

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
        // Build section content first; only emit the header if there's something to show
        var section = new StringBuilder();

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
                    x.Result
                        is { Status: PipelineProgressStatus.Succeeded, Count: not null and > 0 }
                )
                .ToList();

            if (pipelineResults.Count > 0)
            {
                section.AppendLine("Information:");
                section.AppendLine();
                foreach (var (pipelineType, result) in pipelineResults)
                {
                    var description = GetPipelineDescription(pipelineType);
                    // Non-null guaranteed by Where filter above (Count: not null)
                    section.AppendLine(
                        CultureInfo.InvariantCulture,
                        $"- {description}: {result!.Value.Count}"
                    );
                }

                section.AppendLine();
            }
        }

        // Render errors
        var errors = diagnostics.Where(e => e.Level == SyncDiagnosticLevel.Error).ToList();
        if (errors.Count > 0)
        {
            section.AppendLine("Errors:");
            section.AppendLine();
            foreach (var evt in errors)
            {
                section.AppendLine(CultureInfo.InvariantCulture, $"- {evt.Message}");
            }

            section.AppendLine();
        }

        // Render warnings (including deprecations)
        var warnings = diagnostics
            .Where(e => e.Level is SyncDiagnosticLevel.Warning or SyncDiagnosticLevel.Deprecation)
            .ToList();
        if (warnings.Count > 0)
        {
            section.AppendLine("Warnings:");
            section.AppendLine();
            foreach (var evt in warnings)
            {
                var prefix = evt.Level == SyncDiagnosticLevel.Deprecation ? "[DEPRECATED] " : "";
                section.AppendLine(CultureInfo.InvariantCulture, $"- {prefix}{evt.Message}");
            }

            section.AppendLine();
        }

        if (section.Length == 0)
        {
            return;
        }

        if (instanceName == NoInstance)
        {
            body.AppendLine("### General");
        }
        else
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"### Instance: `{instanceName}`");
        }

        body.AppendLine();
        body.Append(section);
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

    // Groups pipeline events by instance, taking the last event per pipeline type as the final
    // status
    private Dictionary<
        string,
        Dictionary<PipelineType, PipelineSnapshot>
    > BuildPipelineSnapshotsByInstance()
    {
        return _pipelines
            .GroupBy(p => p.Instance, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(p => p.Type).ToDictionary(ig => ig.Key, ToSnapshot),
                StringComparer.OrdinalIgnoreCase
            );
    }

    private static PipelineSnapshot ToSnapshot(IGrouping<PipelineType, PipelineEvent> g)
    {
        var last = g.Last();
        return new PipelineSnapshot(last.Status, last.Count);
    }

    public void Dispose() => _subscriptions.Dispose();
}
