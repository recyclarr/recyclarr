using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly Func<ConfigParser> _parserFactory;

    public ConfigurationLoader(Func<ConfigParser> parserFactory)
    {
        _parserFactory = parserFactory;
    }

    public IConfigRegistry LoadMany(IEnumerable<IFileInfo> configFiles, string? desiredSection = null)
    {
        var parser = _parserFactory();

        foreach (var file in configFiles)
        {
            parser.Load(file, desiredSection);
        }

        return parser.Configs;
    }

    public IConfigRegistry Load(IFileInfo file, string? desiredSection = null)
    {
        var parser = _parserFactory();
        parser.Load(file, desiredSection);
        return parser.Configs;
    }

    public IConfigRegistry LoadFromStream(TextReader stream, string? desiredSection = null)
    {
        var parser = _parserFactory();
        parser.LoadFromStream(stream, desiredSection);
        return parser.Configs;
    }
}
