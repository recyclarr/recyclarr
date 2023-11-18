using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigCreationProcessor(IOrderedEnumerable<IConfigCreator> creators) : IConfigCreationProcessor
{
    public void Process(ICreateConfigSettings settings)
    {
        var creator = creators.FirstOrDefault(x => x.CanHandle(settings));
        if (creator is null)
        {
            throw new FatalException("Unable to determine which config creation logic to use");
        }

        creator.Create(settings);
    }
}
