using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "Context objects are similar to DTOs; for usability we want to assign not append"
)]
public class QualityProfilePipelineContext : IPipelineContext
{
    public string PipelineDescription => "Quality Definition";
    public IReadOnlyCollection<SupportedServices> SupportedServiceTypes { get; } =
        [SupportedServices.Sonarr, SupportedServices.Radarr];

    public IList<ProcessedQualityProfileData> ConfigOutput { get; set; } = default!;
    public QualityProfileServiceData ApiFetchOutput { get; set; } = default!;
    public QualityProfileTransactionData TransactionOutput { get; set; } = default!;
}
