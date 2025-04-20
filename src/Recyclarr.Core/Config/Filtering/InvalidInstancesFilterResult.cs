namespace Recyclarr.Config.Filtering;

public class InvalidInstancesFilterResult(
    IReadOnlyCollection<ConfigValidationErrorInfo> invalidInstances
) : IFilterResult
{
    public IReadOnlyCollection<ConfigValidationErrorInfo> InvalidInstances => invalidInstances;
}
