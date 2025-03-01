using Recyclarr.Cli.Pipelines.QualityProfile.Models;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class QualityProfilePipelineContext : PipelineContext
{
    public override string PipelineDescription => "Quality Profile";

    public IList<ProcessedQualityProfileData> ConfigOutput { get; set; } = null!;
    public QualityProfileServiceData ApiFetchOutput { get; set; } = null!;
    public QualityProfileTransactionData TransactionOutput { get; set; } = null!;
}
