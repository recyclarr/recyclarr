namespace Recyclarr.Cli.Pipelines;

/// <summary>
/// Defines a mechanism for state sharing between pipelines.
/// </summary>
internal interface IPipelineCache
{
    void Clear();
}
