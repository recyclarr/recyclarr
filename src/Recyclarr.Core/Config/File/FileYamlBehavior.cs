using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.File;

[UsedImplicitly]
public class FileYamlBehavior(IFileSystem fs, IAppPaths paths) : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new FileDeserializer(fs, paths))
            .WithTagMapping("!file", typeof(FileTag));
    }
}
