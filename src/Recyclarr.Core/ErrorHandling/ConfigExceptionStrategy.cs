using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Parsing.ErrorHandling;

namespace Recyclarr.ErrorHandling;

internal class ConfigExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        HandledInstanceFailure? failure = exception switch
        {
            NoConfigurationFilesException => new NoConfigurationFilesFailure(),
            InvalidInstancesException e => new InvalidInstancesFailure(e.InstanceNames.ToList()),
            DuplicateInstancesException e => new DuplicateInstancesFailure(
                e.InstanceNames.ToList()
            ),
            SplitInstancesException e => new SplitInstancesFailure(e.InstanceNames.ToList()),
            InvalidConfigurationFilesException e => new InvalidConfigurationFilesFailure(
                e.InvalidFiles.Select(file => file.Name).ToList()
            ),
            InvalidConfigurationException => new InvalidConfigurationFailure(),
            PostProcessingException e => new PostProcessingFailure(e.Message),
            _ => null,
        };
        return Task.FromResult(failure);
    }
}
