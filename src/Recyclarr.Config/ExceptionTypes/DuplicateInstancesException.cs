namespace Recyclarr.Config.ExceptionTypes;

public class DuplicateInstancesException : Exception
{
    public IReadOnlyCollection<string> InstanceNames { get; }

    public DuplicateInstancesException(IReadOnlyCollection<string> instanceNames)
    {
        InstanceNames = instanceNames;
    }
}
