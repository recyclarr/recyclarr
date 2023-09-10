using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Yaml;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigSaver
{
    private readonly IYamlSerializerFactory _serializerFactory;

    public ConfigSaver(IYamlSerializerFactory serializerFactory)
    {
        _serializerFactory = serializerFactory;
    }

    public void Save(RootConfigYaml config, IFileInfo destinationFile)
    {
        var serializer = _serializerFactory.CreateSerializer();

        destinationFile.CreateParentDirectory();
        using var stream = destinationFile.CreateText();
        serializer.Serialize(stream, config);
    }
}
