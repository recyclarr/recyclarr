using Recyclarr.Cli.Console.Commands;

namespace Recyclarr.Cli.Console.Setup;

[UsedImplicitly]
internal class CompositeGlobalSetupTask(IOrderedEnumerable<Lazy<IGlobalSetupTask>> tasks)
    : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        foreach (var task in tasks)
        {
            task.Value.OnStart(cmd);
        }
    }

    public void OnFinish()
    {
        foreach (var task in tasks.Reverse())
        {
            task.Value.OnFinish();
        }
    }
}
