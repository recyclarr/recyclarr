using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Generic;

public interface ITransactionPipelinePhase<in TContext>
    where TContext : IPipelineContext
{
    void Execute(TContext context, IServiceConfiguration config);
}
