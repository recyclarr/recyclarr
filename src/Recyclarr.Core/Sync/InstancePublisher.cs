using Recyclarr.Sync.Progress;

namespace Recyclarr.Sync;

public class InstancePublisher(string name, ISyncRunPublisher publisher)
{
    public string Name => name;

    public void SetStatus(InstanceProgressStatus status)
    {
        publisher.Publish(new InstanceEvent(name, status));
    }

    public PipelinePublisher ForPipeline(PipelineType type)
    {
        return new PipelinePublisher(name, type, publisher);
    }
}
