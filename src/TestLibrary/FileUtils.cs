using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;

namespace TestLibrary;

public static class FileUtils
{
    public static ICollection<string> NormalizePaths(IEnumerable<string> paths)
        => paths.Select(NormalizePath).ToList();

    public static string NormalizePath(string path)
    {
        if (MockUnixSupport.IsUnixPlatform())
        {
            return Regex.Replace(path, @"^C:\\", "/").Replace("\\", "/");
        }

        return Regex.Replace(path, @"^/", @"C:\").Replace("/", "\\");
    }
}
