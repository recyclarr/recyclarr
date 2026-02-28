namespace Recyclarr.Cli.Pipelines.MediaNaming.Sonarr;

internal class SonarrNamingTransactionPhase : IPipelinePhase<SonarrNamingPipelineContext>
{
    public Task<PipelineFlow> Execute(SonarrNamingPipelineContext context, CancellationToken ct)
    {
        var configDto = context.Plan.SonarrMediaNaming.Dto;
        context.TransactionOutput = context.ApiFetchOutput with
        {
            RenameEpisodes = configDto.RenameEpisodes,
            SeriesFolderFormat = configDto.SeriesFolderFormat,
            SeasonFolderFormat = configDto.SeasonFolderFormat,
            StandardEpisodeFormat = configDto.StandardEpisodeFormat,
            DailyEpisodeFormat = configDto.DailyEpisodeFormat,
            AnimeEpisodeFormat = configDto.AnimeEpisodeFormat,
        };

        return Task.FromResult(PipelineFlow.Continue);
    }
}
