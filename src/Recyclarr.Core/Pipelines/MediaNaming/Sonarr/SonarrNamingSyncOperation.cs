using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingSyncOperation(ILogger log, ISonarrNamingService api)
    : SyncOperation<SonarrNamingComputeResult>
{
    public override PipelineType Type => PipelineType.MediaNaming;
    public override string Description => "Sonarr Media Naming";

    public override bool ShouldSkip(PipelinePlan plan) => !plan.SonarrMediaNamingAvailable;

    protected override async Task<SonarrNamingComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var current = await api.GetNaming(ct);
        var planned = plan.SonarrMediaNaming.Data;

        // Overlay only non-null planned values; null means "don't change"
        var desired = current with
        {
            RenameEpisodes = planned.RenameEpisodes ?? current.RenameEpisodes,
            SeriesFolderFormat = planned.SeriesFolderFormat ?? current.SeriesFolderFormat,
            SeasonFolderFormat = planned.SeasonFolderFormat ?? current.SeasonFolderFormat,
            StandardEpisodeFormat = planned.StandardEpisodeFormat ?? current.StandardEpisodeFormat,
            DailyEpisodeFormat = planned.DailyEpisodeFormat ?? current.DailyEpisodeFormat,
            AnimeEpisodeFormat = planned.AnimeEpisodeFormat ?? current.AnimeEpisodeFormat,
        };

        return new SonarrNamingComputeResult(current, desired);
    }

    protected override async Task Persist(
        SonarrNamingComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var (current, desired) = computeResult;
        await api.UpdateNaming(desired, ct);

        var differences = current.GetDifferences(desired);

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
}
