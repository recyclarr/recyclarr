using System.IO.Abstractions;
using Recyclarr.Config.Parsing;

namespace Recyclarr.Cli.Processors.Config;

public interface IConfigManipulator
{
    void LoadAndSave(
        IFileInfo source,
        IFileInfo destinationFile,
        Func<string, ServiceConfigYaml, ServiceConfigYaml> editCallback);
}
