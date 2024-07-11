namespace Recyclarr.Cli.Console.Setup;

public class GlobalSetupTaskExecutor(IOrderedEnumerable<IGlobalSetupTask> tasks)
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
