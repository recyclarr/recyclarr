using MoreLinq.Extensions;

namespace Recyclarr.Command.Initialization;

public class ServiceInitializationAndCleanup : IServiceInitializationAndCleanup
{
    private readonly IOrderedEnumerable<IServiceInitializer> _initializers;
    private readonly IOrderedEnumerable<IServiceCleaner> _cleaners;

    public ServiceInitializationAndCleanup(
        IOrderedEnumerable<IServiceInitializer> initializers,
        IOrderedEnumerable<IServiceCleaner> cleaners)
    {
        _initializers = initializers;
        _cleaners = cleaners;
    }

    public async Task Execute(ServiceCommand cmd, Func<Task> logic)
    {
        try
        {
            _initializers.ForEach(x => x.Initialize(cmd));

            await logic();
        }
        finally
        {
            _cleaners.ForEach(x => x.Cleanup());
        }
    }
}
