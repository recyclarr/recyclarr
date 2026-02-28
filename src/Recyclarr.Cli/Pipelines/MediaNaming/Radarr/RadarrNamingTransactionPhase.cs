namespace Recyclarr.Cli.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingTransactionPhase : IPipelinePhase<RadarrNamingPipelineContext>
{
    public Task<PipelineFlow> Execute(RadarrNamingPipelineContext context, CancellationToken ct)
    {
        var planned = context.Plan.RadarrMediaNaming.Data;
        var fetched = context.ApiFetchOutput;

        // Overlay only non-null planned values; null means "don't change"
        context.TransactionOutput = fetched with
        {
            RenameMovies = planned.RenameMovies ?? fetched.RenameMovies,
            StandardMovieFormat = planned.StandardMovieFormat ?? fetched.StandardMovieFormat,
            MovieFolderFormat = planned.MovieFolderFormat ?? fetched.MovieFolderFormat,
        };

        return Task.FromResult(PipelineFlow.Continue);
    }
}
