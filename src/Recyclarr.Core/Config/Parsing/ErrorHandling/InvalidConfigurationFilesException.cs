using System.IO.Abstractions;

namespace Recyclarr.Config.Parsing.ErrorHandling;

public class InvalidConfigurationFilesException(IReadOnlyCollection<IFileInfo> invalidFiles) : Exception
{
    public IReadOnlyCollection<IFileInfo> InvalidFiles { get; } = invalidFiles;
}
