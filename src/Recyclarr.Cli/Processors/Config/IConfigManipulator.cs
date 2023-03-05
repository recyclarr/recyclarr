using System.IO.Abstractions;
using Recyclarr.TrashLib.Config.Parsing;

namespace Recyclarr.Cli.Processors.Config;

public interface IConfigManipulator
{
    void LoadAndSave(
        IFileInfo source,
        IFileInfo destinationFile,
        Func<string, ServiceConfigYaml, ServiceConfigYaml> editCallback);
}
