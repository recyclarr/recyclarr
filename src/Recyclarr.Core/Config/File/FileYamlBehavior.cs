using System.IO.Abstractions;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.File;

[UsedImplicitly]
public class FileYamlBehavior(IFileSystem fs) : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new FileDeserializer(fs))
            .WithTagMapping("!file", typeof(FileTag));
    }
}
