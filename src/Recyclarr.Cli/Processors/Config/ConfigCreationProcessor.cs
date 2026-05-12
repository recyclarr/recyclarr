using Recyclarr.Config;

namespace Recyclarr.Cli.Processors.Config;

internal class ConfigCreationProcessor(IOrderedEnumerable<IConfigCreator> creators)
    : IConfigCreationProcessor
{
    public void Process(ICreateConfigSettings settings)
    {
        var creator =
            creators.FirstOrDefault(x => x.CanHandle(settings))
            ?? throw new FatalException("Unable to determine which config creation logic to use");

        creator.Create(settings);
    }
}
