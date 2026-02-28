namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingTransactionPhase : IPipelinePhase<RadarrNamingPipelineContext>
{
    public Task<PipelineFlow> Execute(RadarrNamingPipelineContext context, CancellationToken ct)
    {
        var configDto = context.Plan.RadarrMediaNaming.Dto;
        context.TransactionOutput = context.ApiFetchOutput with
        {
            RenameMovies = configDto.RenameMovies,
            MovieFolderFormat = configDto.MovieFolderFormat,
            StandardMovieFormat = configDto.StandardMovieFormat,
        };

        return Task.FromResult(PipelineFlow.Continue);
    }
}
