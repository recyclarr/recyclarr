namespace Recyclarr.Config.ExceptionTypes;

public class InvalidInstancesException(IReadOnlyCollection<string> instanceNames) : Exception
{
    public IReadOnlyCollection<string> InstanceNames { get; } = instanceNames;
}
