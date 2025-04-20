namespace Recyclarr.Config.Filtering;

public class DuplicateInstancesFilterResult(IReadOnlyCollection<string> duplicateInstances)
    : IFilterResult
{
    public IReadOnlyCollection<string> DuplicateInstances => duplicateInstances;
}
