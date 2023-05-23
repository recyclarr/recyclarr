using System.Collections.Immutable;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Startup;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Secrets;

public class SecretsProvider : ISecretsProvider
{
    public IImmutableDictionary<string, string> Secrets => _secrets.Value;

    private readonly IAppPaths _paths;
    private readonly Lazy<IImmutableDictionary<string, string>> _secrets;

    public SecretsProvider(IAppPaths paths)
    {
        _paths = paths;
        _secrets = new Lazy<IImmutableDictionary<string, string>>(LoadSecretsFile);
    }

    private IImmutableDictionary<string, string> LoadSecretsFile()
    {
        var result = new Dictionary<string, string>();

        var yamlPath = _paths.AppDataDirectory.YamlFile("secrets");
        if (yamlPath is not null)
        {
            using var stream = yamlPath.OpenText();
            var deserializer = new DeserializerBuilder().Build();
            var dict = deserializer.Deserialize<Dictionary<string, string>?>(stream);
            if (dict is not null)
            {
                result = dict;
            }
        }

        return result.ToImmutableDictionary();
    }
}
