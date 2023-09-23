namespace Recyclarr.Config.ExceptionTypes;

public class SplitInstancesException : Exception
{
    public IReadOnlyCollection<string> InstanceNames { get; }

    public SplitInstancesException(IReadOnlyCollection<string> instanceNames)
    {
        InstanceNames = instanceNames;
    }
}
