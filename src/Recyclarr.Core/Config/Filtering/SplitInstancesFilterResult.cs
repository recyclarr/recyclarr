namespace Recyclarr.Config.Filtering;

public class SplitInstancesFilterResult(IReadOnlyCollection<SplitInstanceErrorInfo> splitInstances)
    : IFilterResult
{
    public IReadOnlyCollection<SplitInstanceErrorInfo> SplitInstances => splitInstances;
}
