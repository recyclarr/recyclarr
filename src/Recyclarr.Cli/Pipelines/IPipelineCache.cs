namespace Recyclarr.Cli.Pipelines;

/// <summary>
/// Defines a mechanism for state sharing between pipelines.
/// </summary>
public interface IPipelineCache
{
    void Clear();
}
