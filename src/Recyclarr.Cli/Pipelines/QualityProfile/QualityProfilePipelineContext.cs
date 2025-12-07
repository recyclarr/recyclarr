using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class QualityProfilePipelineContext : PipelineContext
{
    public override string PipelineDescription => "Quality Profile";
    public override PipelineType PipelineType => PipelineType.QualityProfile;

    public QualityProfileServiceData ApiFetchOutput { get; set; } = null!;
    public QualityProfileTransactionData TransactionOutput { get; set; } = null!;
}
