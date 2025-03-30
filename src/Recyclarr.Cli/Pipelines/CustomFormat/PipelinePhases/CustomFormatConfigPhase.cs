using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatConfigPhase(
    ICustomFormatGuideService guide,
    ProcessedCustomFormatCache cache,
    ICachePersister<CustomFormatCache> cachePersister,
    IServiceConfiguration config
) : IPipelinePhase<CustomFormatPipelineContext>
{
    public Task<PipelineFlow> Execute(CustomFormatPipelineContext context, CancellationToken ct)
    {
        // Match custom formats in the YAML config to those in the guide, by Trash ID
        //
        // This solution is conservative: CustomFormatData is only created for CFs in the guide that are
        // specified in the config.
        //
        // The ToLookup() at the end finds TrashIDs provided in the config that do not match anything in the guide.
        // These will yield a warning in the logs.
        var processedCfs = config
            .CustomFormats.SelectMany(x => x.TrashIds)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .GroupJoin(
                guide.GetCustomFormatData(config.ServiceType),
                x => x,
                x => x.TrashId,
                (id, cf) => (Id: id, CustomFormats: cf)
            )
            .ToLookup(x => x.Item2.Any());

        context.InvalidFormats = processedCfs[false].Select(x => x.Id).ToList();
        context.ConfigOutput.AddRange(processedCfs[true].SelectMany(x => x.CustomFormats));
        context.Cache = cachePersister.Load();

        cache.AddCustomFormats(context.ConfigOutput);

        return Task.FromResult(PipelineFlow.Continue);
    }
}
