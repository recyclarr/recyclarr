using System.IO.Abstractions;

namespace Recyclarr.Platform;

public interface IAppPaths
{
    IDirectoryInfo ConfigDirectory { get; }
    IDirectoryInfo CliLogDirectory { get; }
    IDirectoryInfo ServerLogDirectory { get; }
    IDirectoryInfo ResourceDirectory { get; }
    IDirectoryInfo StateDirectory { get; }
    IDirectoryInfo YamlConfigDirectory { get; }
    IDirectoryInfo YamlIncludeDirectory { get; }
}
