using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.TrashGuide;

namespace Recyclarr.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingSyncOperation(
    ILogger log,
    IRadarrNamingService api,
    IEnumerable<IPreviewRenderer<RadarrNamingData>> previewRenderers
) : ISyncOperation
{
    private RadarrNamingData _apiFetchOutput = null!;
    private RadarrNamingData _transactionOutput = null!;

    public PipelineType Type => PipelineType.MediaNaming;
    public string Description => "Radarr Media Naming";
    public IReadOnlyList<PipelineType> Dependencies => [];

    public bool ShouldSkip(PipelinePlan plan, SupportedServices serviceType) =>
        serviceType != SupportedServices.Radarr || !plan.RadarrMediaNamingAvailable;

    public async Task Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct)
    {
        // Fetch phase
        _apiFetchOutput = await api.GetNaming(ct);

        // Transaction phase
        var planned = plan.RadarrMediaNaming.Data;
        var fetched = _apiFetchOutput;

        // Overlay only non-null planned values; null means "don't change"
        _transactionOutput = fetched with
        {
            RenameMovies = planned.RenameMovies ?? fetched.RenameMovies,
            StandardMovieFormat = planned.StandardMovieFormat ?? fetched.StandardMovieFormat,
            MovieFolderFormat = planned.MovieFolderFormat ?? fetched.MovieFolderFormat,
        };
    }

    public async Task Persist(IPipelinePublisher publisher, CancellationToken ct)
    {
        await api.UpdateNaming(_transactionOutput, ct);

        var differences = _apiFetchOutput.GetDifferences(_transactionOutput);

        if (differences.Count != 0)
        {
            log.Information("Media naming has been updated");
            log.Debug("Naming differences: {Diff}", differences);
        }
        else
        {
            log.Information("Media naming is up to date!");
        }

        publisher.SetStatus(PipelineProgressStatus.Succeeded, differences.Count);
    }

    public void RenderPreview(string instanceName)
    {
        var renderer = previewRenderers.FirstOrDefault();
        renderer?.Render(Description, instanceName, _transactionOutput);
    }
}
