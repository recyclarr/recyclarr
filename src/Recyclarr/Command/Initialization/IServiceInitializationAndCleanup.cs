namespace Recyclarr.Command.Initialization;

public interface IServiceInitializationAndCleanup
{
    Task Execute(ServiceCommand cmd, Func<Task> logic);
}
