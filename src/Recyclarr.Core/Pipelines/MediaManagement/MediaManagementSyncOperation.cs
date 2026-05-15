using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Pipelines.MediaManagement;

internal class MediaManagementSyncOperation(
    ILogger log,
    IMediaManagementService api,
    IEnumerable<IPreviewRenderer<MediaManagementData>> previewRenderers
) : ISyncOperation
{
    private MediaManagementData _apiFetchOutput = null!;
    private MediaManagementData _transactionOutput = null!;

    public PipelineType Type => PipelineType.MediaManagement;
    public string Description => "Media Management";
    public IReadOnlyList<PipelineType> Dependencies => [];

    public bool ShouldSkip(PipelinePlan plan) => !plan.MediaManagementAvailable;

    public async Task Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct)
    {
        // Fetch phase
        _apiFetchOutput = await api.GetMediaManagement(ct);

        // Transaction phase
        var planned = plan.MediaManagement;

        _transactionOutput = _apiFetchOutput with { PropersAndRepacks = planned.PropersAndRepacks };
    }

    public async Task Persist(IPipelinePublisher publisher, CancellationToken ct)
    {
        var differences = _apiFetchOutput.GetDifferences(_transactionOutput);

        if (differences.Count != 0)
        {
            await api.UpdateMediaManagement(_transactionOutput, ct);
            log.Information("Media management has been updated");
            log.Debug("Media management differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media management is up to date!");
        }

        publisher.SetStatus(PipelineProgressStatus.Succeeded, differences.Count);
    }

    public void RenderPreview(string instanceName)
    {
        var renderer = previewRenderers.FirstOrDefault();
        renderer?.Render(Description, instanceName, _transactionOutput);
    }
}
