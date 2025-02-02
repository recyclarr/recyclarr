namespace Recyclarr.Config.ExceptionTypes;

public class DuplicateInstancesException(IReadOnlyCollection<string> instanceNames)
    : InvalidConfigurationException
{
    public IReadOnlyCollection<string> InstanceNames { get; } = instanceNames;
}
