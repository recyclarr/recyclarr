using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Cache;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiFetchPhase(
    IQualityProfileApiService api,
    ICachePersister<QualityProfileCache> cachePersister
) : IPipelinePhase<QualityProfilePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualityProfilePipelineContext context,
        CancellationToken ct
    )
    {
        var profilesTask = api.GetQualityProfiles(ct);
        var schemaTask = api.GetSchema(ct);
        var languagesTask = api.GetLanguages(ct);
        await Task.WhenAll(profilesTask, schemaTask, languagesTask);

        context.ApiFetchOutput = new QualityProfileServiceData(
            (await profilesTask).AsReadOnly(),
            await schemaTask,
            (await languagesTask).AsReadOnly()
        );
        context.Cache = cachePersister.Load();
        return PipelineFlow.Continue;
    }
}
