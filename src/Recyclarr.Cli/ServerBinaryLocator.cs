using System.IO.Abstractions;

namespace Recyclarr.Cli;

/// <summary>
/// Locates the <c>recyclarr-server</c> binary relative to the running CLI process. Derives
/// the platform-specific extension from the CLI's own process path rather than branching on
/// <see cref="OperatingSystem"/>.
/// </summary>
internal static class ServerBinaryLocator
{
    public static IFileInfo GetServerBinary(IFileSystem fs)
    {
        // non-null: ProcessPath is only null in bundled single-file hosts without apphost
        var processPath = Environment.ProcessPath!;
        var processDir = fs.FileInfo.New(processPath).Directory!;
        var extension = Path.GetExtension(processPath);
        return processDir.File($"recyclarr-server{extension}");
    }
}
