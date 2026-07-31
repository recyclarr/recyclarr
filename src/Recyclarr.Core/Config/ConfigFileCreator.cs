namespace Recyclarr.Config;

internal class ConfigFileCreator(IOrderedEnumerable<IConfigCreator> creators) : IConfigFileCreator
{
    public IReadOnlyList<CreatedConfigFile> Create(ICreateConfigSettings settings)
    {
        var creator =
            creators.FirstOrDefault(x => x.CanHandle(settings))
            ?? throw new FatalException("Unable to determine which config creation logic to use");

        return creator.Create(settings);
    }
}
