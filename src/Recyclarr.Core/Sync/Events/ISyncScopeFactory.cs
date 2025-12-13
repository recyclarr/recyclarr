namespace Recyclarr.Sync.Events;

public interface ISyncScopeFactory
{
    IDisposable SetInstance(string instanceName);
    IDisposable SetPipeline(PipelineType pipeline);
}
