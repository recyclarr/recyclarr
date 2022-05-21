namespace Recyclarr.Command.Initialization;

public interface IServiceInitializationAndCleanup
{
    Task Execute(IServiceCommand cmd, Func<Task> logic);
}
