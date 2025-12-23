namespace Recyclarr.Cli.Pipelines;

/// <summary>
/// Thrown when a pipeline encounters blocking errors and must stop execution.
/// Errors are already recorded via AddError() before this is thrown.
/// </summary>
internal class PipelineInterruptException : Exception;
