using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigCreationProcessor : IConfigCreationProcessor
{
    private readonly IOrderedEnumerable<IConfigCreator> _creators;

    public ConfigCreationProcessor(IOrderedEnumerable<IConfigCreator> creators)
    {
        _creators = creators;
    }

    public void Process(ICreateConfigSettings settings)
    {
        var creator = _creators.FirstOrDefault(x => x.CanHandle(settings));
        if (creator is null)
        {
            throw new FatalException("Unable to determine which config creation logic to use");
        }

        creator.Create(settings);
    }
}
