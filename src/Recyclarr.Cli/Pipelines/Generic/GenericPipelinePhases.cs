namespace Recyclarr.Cli.Pipelines.Generic;

public class GenericPipelinePhases<TContext>
    where TContext : IPipelineContext
{
    public required IConfigPipelinePhase<TContext> ConfigPhase { get; init; }
    public required ILogPipelinePhase<TContext> LogPhase { get; init; }
    public required IApiFetchPipelinePhase<TContext> ApiFetchPhase { get; init; }
    public required ITransactionPipelinePhase<TContext> TransactionPhase { get; init; }
    public required IPreviewPipelinePhase<TContext> PreviewPhase { get; init; }
    public required IApiPersistencePipelinePhase<TContext> ApiPersistencePhase { get; init; }
}
