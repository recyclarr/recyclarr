using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class ConfiguredCustomFormatProvider(
    IServiceConfiguration config,
    QualityProfileResourceQuery qpQuery
)
{
    public IEnumerable<CustomFormatConfig> GetAll()
    {
        var qpResources = qpQuery
            .Get(config.ServiceType)
            .ToDictionary(r => r.TrashId, StringComparer.OrdinalIgnoreCase);

        // Synthesize CustomFormatConfig from QP formatItems
        var fromFormatItems = config
            .QualityProfiles.Where(qp => qp.TrashId is not null)
            .Select(qp => (Config: qp, Resource: qpResources.GetValueOrDefault(qp.TrashId!)))
            .Where(x => x.Resource?.FormatItems.Count > 0)
            .Select(x => new CustomFormatConfig
            {
                TrashIds = x.Resource!.FormatItems.Values.ToList(),
                AssignScoresTo =
                [
                    new AssignScoresToConfig
                    {
                        Name = !string.IsNullOrEmpty(x.Config.Name)
                            ? x.Config.Name
                            : x.Resource!.Name,
                    },
                ],
            });

        return config.CustomFormats.Concat(fromFormatItems);
    }
}
