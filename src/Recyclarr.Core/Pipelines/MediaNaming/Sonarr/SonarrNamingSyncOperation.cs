using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingSyncOperation(
    ILogger log,
    ISonarrNamingService api,
    IEnumerable<IPreviewRenderer<SonarrNamingData>> previewRenderers
) : ISyncOperation
{
    private SonarrNamingData _apiFetchOutput = null!;
    private SonarrNamingData _transactionOutput = null!;

    public PipelineType Type => PipelineType.MediaNaming;
    public string Description => "Sonarr Media Naming";
    public IReadOnlyList<PipelineType> Dependencies => [];

    public bool ShouldSkip(PipelinePlan plan) => !plan.SonarrMediaNamingAvailable;

    public async Task Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct)
    {
        // Fetch phase
        _apiFetchOutput = await api.GetNaming(ct);

        // Transaction phase
        var planned = plan.SonarrMediaNaming.Data;
        var fetched = _apiFetchOutput;

        // Overlay only non-null planned values; null means "don't change"
        _transactionOutput = fetched with
        {
            RenameEpisodes = planned.RenameEpisodes ?? fetched.RenameEpisodes,
            SeriesFolderFormat = planned.SeriesFolderFormat ?? fetched.SeriesFolderFormat,
            SeasonFolderFormat = planned.SeasonFolderFormat ?? fetched.SeasonFolderFormat,
            StandardEpisodeFormat = planned.StandardEpisodeFormat ?? fetched.StandardEpisodeFormat,
            DailyEpisodeFormat = planned.DailyEpisodeFormat ?? fetched.DailyEpisodeFormat,
            AnimeEpisodeFormat = planned.AnimeEpisodeFormat ?? fetched.AnimeEpisodeFormat,
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
