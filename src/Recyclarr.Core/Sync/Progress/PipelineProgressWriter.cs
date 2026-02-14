namespace Recyclarr.Sync.Progress;

public class PipelineProgressWriter(Action<PipelineProgressStatus, int?> setStatus)
{
    public void SetStatus(PipelineProgressStatus status, int? count = null)
    {
        setStatus(status, count);
    }
}
