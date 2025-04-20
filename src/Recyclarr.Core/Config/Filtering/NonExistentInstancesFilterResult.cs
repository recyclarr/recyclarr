namespace Recyclarr.Config.Filtering;

public class NonExistentInstancesFilterResult(IReadOnlyCollection<string> nonExistentInstances)
    : IFilterResult
{
    public IReadOnlyCollection<string> NonExistentInstances => nonExistentInstances;
}
