namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingTransactionPhase : IPipelinePhase<SonarrNamingPipelineContext>
{
    public Task<PipelineFlow> Execute(SonarrNamingPipelineContext context, CancellationToken ct)
    {
        var planned = context.Plan.SonarrMediaNaming.Data;
        var fetched = context.ApiFetchOutput;

        // Overlay only non-null planned values; null means "don't change"
        context.TransactionOutput = fetched with
        {
            RenameEpisodes = planned.RenameEpisodes ?? fetched.RenameEpisodes,
            SeriesFolderFormat = planned.SeriesFolderFormat ?? fetched.SeriesFolderFormat,
            SeasonFolderFormat = planned.SeasonFolderFormat ?? fetched.SeasonFolderFormat,
            StandardEpisodeFormat = planned.StandardEpisodeFormat ?? fetched.StandardEpisodeFormat,
            DailyEpisodeFormat = planned.DailyEpisodeFormat ?? fetched.DailyEpisodeFormat,
            AnimeEpisodeFormat = planned.AnimeEpisodeFormat ?? fetched.AnimeEpisodeFormat,
        };

        return Task.FromResult(PipelineFlow.Continue);
    }
}
