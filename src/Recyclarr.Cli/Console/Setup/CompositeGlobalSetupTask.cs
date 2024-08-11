namespace Recyclarr.Cli.Console.Setup;

[UsedImplicitly]
public class CompositeGlobalSetupTask(IOrderedEnumerable<IGlobalSetupTask> tasks) : IGlobalSetupTask
{
    public void OnStart()
    {
        tasks.ForEach(x => x.OnStart());
    }

    public void OnFinish()
    {
        tasks.Reverse().ForEach(x => x.OnFinish());
    }
}
