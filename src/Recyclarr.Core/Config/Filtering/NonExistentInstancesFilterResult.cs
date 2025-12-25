namespace Recyclarr.Config.Filtering;

public class NonExistentInstancesFilterResult(
    IReadOnlyCollection<string> nonExistentInstances,
    IReadOnlyCollection<string> availableInstances
) : IFilterResult
{
    public IReadOnlyCollection<string> NonExistentInstances => nonExistentInstances;
    public IReadOnlyCollection<string> AvailableInstances => availableInstances;
}
