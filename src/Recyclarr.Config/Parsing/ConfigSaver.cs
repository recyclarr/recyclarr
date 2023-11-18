using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Yaml;

namespace Recyclarr.Config.Parsing;

public class ConfigSaver(IYamlSerializerFactory serializerFactory)
{
    public void Save(RootConfigYaml config, IFileInfo destinationFile)
    {
        var serializer = serializerFactory.CreateSerializer();

        destinationFile.CreateParentDirectory();
        using var stream = destinationFile.CreateText();
        serializer.Serialize(stream, config);
    }
}
