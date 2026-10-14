using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.State;
using Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases.Limits;
using Recyclarr.Config.Models;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

internal class QualityProfileApiFetchPhase(
    IQualityProfileService service,
    IQualityProfileStatePersister statePersister,
    IQualityItemLimitFactory limitFactory,
    IServiceConfiguration config
) : IPipelinePhase<QualityProfilePipelineContext>
{
    public async Task<PipelineFlow> Execute(
        QualityProfilePipelineContext context,
        CancellationToken ct
    )
    {
        var profilesTask = service.GetQualityProfiles(ct);
        var schemaTask = service.GetSchema(ct);
        var languagesTask = service.GetLanguages(ct);
        await Task.WhenAll(profilesTask, schemaTask, languagesTask);

        context.ApiFetchOutput = new QualityProfileServiceData(
            await profilesTask,
            await schemaTask,
            await languagesTask
        );
        context.State = statePersister.Load();

        if (context.Plan.QualityProfiles.Any(x => x.QualitySizes is not null))
        {
            context.SizeLimits = await limitFactory.Create(config.ServiceType, ct);
        }

        return PipelineFlow.Continue;
    }
}
