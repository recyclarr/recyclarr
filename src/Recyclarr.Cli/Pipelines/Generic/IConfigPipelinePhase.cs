using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.Generic;

public interface IConfigPipelinePhase<in TContext>
{
    Task Execute(TContext context, IServiceConfiguration config);
}