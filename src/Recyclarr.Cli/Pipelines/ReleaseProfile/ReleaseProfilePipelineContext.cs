using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Models;
using Recyclarr.Cli.Pipelines.ReleaseProfile.PipelinePhases;
using Recyclarr.Common;
using Recyclarr.ServarrApi.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile;

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification =
    "Context objects are similar to DTOs; for usability we want to assign not append")]
public class ReleaseProfilePipelineContext : IPipelineContext
{
    public string PipelineDescription => "Release Profile Pipeline";
    public IReadOnlyCollection<SupportedServices> SupportedServiceTypes { get; } = new[]
    {
        SupportedServices.Sonarr
    };

    public IList<ProcessedReleaseProfileData> ConfigOutput { get; set; } = default!;
    public IList<SonarrReleaseProfile> ApiFetchOutput { get; set; } = default!;
    public ReleaseProfileTransactionData TransactionOutput { get; set; } = default!;
}
