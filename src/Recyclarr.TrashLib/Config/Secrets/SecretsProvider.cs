using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Startup;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Secrets;

public class SecretsProvider : ISecretsProvider
{
    public IReadOnlyDictionary<string, string> Secrets => _secrets.Value;

    private readonly IAppPaths _paths;
    private readonly Lazy<Dictionary<string, string>> _secrets;

    public SecretsProvider(IAppPaths paths)
    {
        _paths = paths;
        _secrets = new Lazy<Dictionary<string, string>>(LoadSecretsFile);
    }

    private Dictionary<string, string> LoadSecretsFile()
    {
        var yamlPath = _paths.AppDataDirectory.YamlFile("secrets");
        if (yamlPath is null)
        {
            return new Dictionary<string, string>();
        }

        using var stream = yamlPath.OpenText();
        var deserializer = new DeserializerBuilder().Build();
        var result = deserializer.Deserialize<Dictionary<string, string>?>(stream);
        return result ?? new Dictionary<string, string>();
    }
}
