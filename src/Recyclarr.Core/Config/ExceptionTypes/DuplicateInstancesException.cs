namespace Recyclarr.Config.ExceptionTypes;

public class DuplicateInstancesException(IReadOnlyCollection<string> instanceNames) : Exception
{
    public IReadOnlyCollection<string> InstanceNames { get; } = instanceNames;
}
