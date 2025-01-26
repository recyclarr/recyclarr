namespace Recyclarr.Config.ExceptionTypes;

public class SplitInstancesException(IReadOnlyCollection<string> instanceNames)
    : InvalidConfigurationException
{
    public IReadOnlyCollection<string> InstanceNames { get; } = instanceNames;
}
