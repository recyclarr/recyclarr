using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Parsing.ErrorHandling;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class ConfigExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        var message = exception switch
        {
            NoConfigurationFilesException => "No configuration files found",
            InvalidInstancesException e =>
                $"Invalid instances: {string.Join(", ", e.InstanceNames)}",
            DuplicateInstancesException e =>
                $"Duplicate instance names: {string.Join(", ", e.InstanceNames)}",
            SplitInstancesException e =>
                $"Configs sharing base_url not allowed: {string.Join(", ", e.InstanceNames)}",
            InvalidConfigurationFilesException e =>
                $"Config files not found: {string.Join(", ", e.InvalidFiles.Select(f => f.Name))}",
            InvalidConfigurationException => "One or more invalid configurations found",
            PostProcessingException e => e.Message,
            _ => null,
        };
        return Task.FromResult<IReadOnlyList<string>?>(message is not null ? [message] : null);
    }
}
