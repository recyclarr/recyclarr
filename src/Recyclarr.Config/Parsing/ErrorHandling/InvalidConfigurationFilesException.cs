using System.IO.Abstractions;

namespace Recyclarr.Config.Parsing.ErrorHandling;

public class InvalidConfigurationFilesException : Exception
{
    public IReadOnlyCollection<IFileInfo> InvalidFiles { get; }

    public InvalidConfigurationFilesException(IReadOnlyCollection<IFileInfo> invalidFiles)
    {
        InvalidFiles = invalidFiles;
    }
}
