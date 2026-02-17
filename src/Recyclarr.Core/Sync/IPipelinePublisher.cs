using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public interface IPipelinePublisher
{
    static IPipelinePublisher Noop { get; } = new NoopPipelinePublisher();

    void SetStatus(PipelineProgressStatus status, int? count = null);
    void AddError(string message);
    void AddWarning(string message);
    void AddDeprecation(string message);
}

internal sealed class NoopPipelinePublisher : IPipelinePublisher
{
    public static NoopPipelinePublisher Instance { get; } = new();

    public void SetStatus(PipelineProgressStatus status, int? count = null) { }

    public void AddError(string message) { }

    public void AddWarning(string message) { }

    public void AddDeprecation(string message) { }
}
