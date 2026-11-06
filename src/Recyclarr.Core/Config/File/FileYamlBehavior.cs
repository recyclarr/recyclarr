using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Platform;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.File;

public class FileYamlBehavior(IFileSystem fs, IAppPaths paths, ConfigInputRecorder inputs)
    : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new FileDeserializer(fs, paths, inputs))
            .WithTagMapping("!file", typeof(FileTag));
    }
}
