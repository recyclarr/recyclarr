using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Radarr;

internal class RadarrNamingSync(ILogger log, IRadarrNamingService api) : IMediaNamingSync
{
    public PipelineResult CreateEmptyResult() => new RadarrNamingPipelineResult(0, 0, [], null);

    public async Task<IPipelineResultSource> Compute(
        PlannedMediaNaming plannedNaming,
        CancellationToken ct
    )
    {
        // DI resolves this sync only for Radarr instances, whose planner plans Radarr naming
        var radarrNaming = (PlannedRadarrMediaNaming)plannedNaming;
        var planned = radarrNaming.Data;
        var outcomes = radarrNaming.Mismatches.Cast<RadarrNamingOutcome>().ToList();
        var completedFields = CountConfiguredFields(planned);
        var incompleteFields = outcomes.Count;
        if (completedFields == 0)
        {
            var failedResult = new RadarrNamingPipelineResult(0, incompleteFields, outcomes, null);
            return new RadarrNamingComputeResult(null, null, failedResult);
        }

        var current = await api.GetNaming(ct);

        // Overlay only non-null planned values; null means "don't change"
        var desired = current with
        {
            RenameMovies = planned.RenameMovies ?? current.RenameMovies,
            StandardMovieFormat = planned.StandardMovieFormat ?? current.StandardMovieFormat,
            MovieFolderFormat = planned.MovieFolderFormat ?? current.MovieFolderFormat,
        };

        var delta = BuildDelta(current, desired, planned);
        var result = new RadarrNamingPipelineResult(
            completedFields,
            incompleteFields,
            outcomes,
            delta
        );
        return new RadarrNamingComputeResult(current, desired, result);
    }

    public async Task Persist(IPipelineResultSource computeResult, CancellationToken ct)
    {
        var (current, desired, result) = (RadarrNamingComputeResult)computeResult;
        if (result.Delta is null)
        {
            return;
        }

        if (current is null || desired is null)
        {
            throw new InvalidOperationException("Changed naming result requires transaction data");
        }

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
    }

    private static RadarrNamingDelta? BuildDelta(
        RadarrNamingData current,
        RadarrNamingData desired,
        RadarrNamingData planned
    )
    {
        var changes = new ConfiguredValueChanges<RadarrNamingData>(current, desired, planned);
        var delta = new RadarrNamingDelta
        {
            RenameMovies = changes.For(x => x.RenameMovies),
            StandardMovieFormat = changes.For(x => x.StandardMovieFormat),
            MovieFolderFormat = changes.For(x => x.MovieFolderFormat),
        };

        return delta == new RadarrNamingDelta() ? null : delta;
    }

    private static int CountConfiguredFields(RadarrNamingData planned)
    {
        var count = 0;
        count += planned.RenameMovies is not null ? 1 : 0;
        count += planned.StandardMovieFormat is not null ? 1 : 0;
        count += planned.MovieFolderFormat is not null ? 1 : 0;
        return count;
    }
}
