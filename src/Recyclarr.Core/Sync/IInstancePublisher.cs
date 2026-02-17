using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public interface IDiagnosticPublisher
{
    void AddError(string message);
    void AddWarning(string message);
    void AddDeprecation(string message);
}

public interface IInstancePublisher : IDiagnosticPublisher
{
    static IInstancePublisher Noop { get; } = new NoopInstancePublisher();

    string Name { get; }
    void SetStatus(InstanceProgressStatus status);
    IPipelinePublisher ForPipeline(PipelineType type);
}

internal sealed class NoopInstancePublisher : IInstancePublisher
{
    public string Name => "";

    public void SetStatus(InstanceProgressStatus status) { }

    public void AddError(string message) { }

    public void AddWarning(string message) { }

    public void AddDeprecation(string message) { }

    public IPipelinePublisher ForPipeline(PipelineType type)
    {
        return NoopPipelinePublisher.Instance;
    }
}
