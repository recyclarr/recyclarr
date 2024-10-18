namespace Recyclarr.Config.ExceptionTypes;

public class SplitInstancesException(IReadOnlyCollection<string> instanceNames) : Exception
{
    public IReadOnlyCollection<string> InstanceNames { get; } = instanceNames;
}
