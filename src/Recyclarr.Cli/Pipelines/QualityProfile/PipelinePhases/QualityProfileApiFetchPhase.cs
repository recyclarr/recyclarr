using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiFetchPhase(
    IQualityProfileApiService api,
    ISyncStatePersister<QualityProfileMappings> statePersister
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
        context.State = statePersister.Load();
        return PipelineFlow.Continue;
    }
}
