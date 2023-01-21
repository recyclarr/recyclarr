using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;

namespace Recyclarr.TestLibrary;

public static partial class FileUtils
{
    public static ICollection<string> NormalizePaths(IEnumerable<string> paths)
        => paths.Select(NormalizePath).ToList();

    public static string NormalizePath(string path)
    {
        return MockUnixSupport.IsUnixPlatform()
            ? WindowsRootRegex().Replace(path, "/").Replace("\\", "/")
            : LinuxRootRegex().Replace(path, @"C:\").Replace("/", "\\");
    }

    [GeneratedRegex(@"^C:\\")]
    private static partial Regex WindowsRootRegex();

    [GeneratedRegex("^/")]
    private static partial Regex LinuxRootRegex();
}
