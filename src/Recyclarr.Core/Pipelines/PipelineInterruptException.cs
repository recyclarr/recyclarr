using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Pipelines;

/// <summary>
/// Thrown when a pipeline encounters blocking errors and must stop execution.
/// Errors are already recorded via AddError() before this is thrown.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1064:Exceptions should be public",
    Justification = "Internal signal between pipeline infrastructure; never crosses assembly boundary"
)]
internal class PipelineInterruptException : Exception;
