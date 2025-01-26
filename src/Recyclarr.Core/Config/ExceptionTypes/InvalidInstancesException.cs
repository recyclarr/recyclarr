namespace Recyclarr.Config.ExceptionTypes;

public class InvalidInstancesException(IReadOnlyCollection<string> instanceNames)
    : InvalidConfigurationException
{
    public IReadOnlyCollection<string> InstanceNames { get; } = instanceNames;
}
