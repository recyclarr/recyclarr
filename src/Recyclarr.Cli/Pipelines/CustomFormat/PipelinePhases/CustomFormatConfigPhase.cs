using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatConfigPhase(ILogger log, ICustomFormatGuideService guide, ProcessedCustomFormatCache cache)
{
    public IReadOnlyCollection<CustomFormatData> Execute(IServiceConfiguration config)
    {
        // Match custom formats in the YAML config to those in the guide, by Trash ID
        //
        // This solution is conservative: CustomFormatData is only created for CFs in the guide that are
        // specified in the config.
        //
        // The ToLookup() at the end finds TrashIDs provided in the config that do not match anything in the guide.
        // These will yield a warning in the logs.
        var processedCfs = config.CustomFormats
            .SelectMany(x => x.TrashIds)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .GroupJoin(guide.GetCustomFormatData(config.ServiceType),
                x => x,
                x => x.TrashId,
                (id, cf) => (Id: id, CustomFormats: cf))
            .ToLookup(x => x.Item2.Any());

        var invalidCfs = processedCfs[false].Select(x => x.Id).ToList();
        if (invalidCfs.IsNotEmpty())
        {
            log.Warning("These Custom Formats do not exist in the guide and will be skipped: {Cfs}", invalidCfs);
        }

        var validCfs = processedCfs[true].SelectMany(x => x.CustomFormats).ToList();
        cache.AddCustomFormats(validCfs);
        return validCfs;
    }
}
