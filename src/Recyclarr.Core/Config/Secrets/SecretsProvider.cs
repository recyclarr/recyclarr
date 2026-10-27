using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Secrets;

public class SecretsProvider(IAppPaths paths) : ISecretsProvider
{
    public IReadOnlyDictionary<string, string> Secrets => _secrets.Value;

    private readonly Lazy<Dictionary<string, string>> _secrets = new(() =>
        Load(paths.ConfigDirectory)
    );

    /// <summary>
    /// The secrets in <c>secrets.yml</c> of the configuration directory; empty without the file.
    /// </summary>
    public static Dictionary<string, string> Load(IDirectoryInfo configDirectory)
    {
        var yamlPath = configDirectory.YamlFile("secrets");
        if (yamlPath is null)
        {
            return [];
        }

        using var stream = yamlPath.OpenText();
        var deserializer = new DeserializerBuilder().Build();
        var result = deserializer.Deserialize<Dictionary<string, string>?>(stream);
        return result ?? [];
    }
}
