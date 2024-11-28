using System.Text.RegularExpressions;

namespace Recyclarr.TestLibrary;

public static partial class FileUtils
{
    public static ICollection<string> NormalizePaths(IEnumerable<string> paths)
    {
        return paths.Select(NormalizePath).ToList();
    }

    public static string NormalizePath(string path)
    {
        return MockUnixSupport.IsUnixPlatform()
            ? WindowsRootRegex().Replace(path, "/").Replace("\\", "/", StringComparison.Ordinal)
            : LinuxRootRegex().Replace(path, @"C:\").Replace("/", "\\", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^C:\\", RegexOptions.None, 1000)]
    private static partial Regex WindowsRootRegex();

    [GeneratedRegex("^/", RegexOptions.None, 1000)]
    private static partial Regex LinuxRootRegex();
}
