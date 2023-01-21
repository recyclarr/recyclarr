using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigurationLoader
{
    IConfigRegistry LoadMany(IEnumerable<IFileInfo> configFiles, string? desiredSection = null);
    IConfigRegistry Load(IFileInfo file, string? desiredSection = null);
    IConfigRegistry LoadFromStream(TextReader stream, string? desiredSection = null);
}
