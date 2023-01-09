using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigurationLoader
{
    IConfigCollection LoadMany(IEnumerable<IFileInfo> configFiles, string? desiredSection = null);
    IConfigCollection Load(IFileInfo file, string? desiredSection = null);
    IConfigCollection LoadFromStream(TextReader stream, string? desiredSection = null);
}
