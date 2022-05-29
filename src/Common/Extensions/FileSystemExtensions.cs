using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Common.Extensions;

public static class FileSystemExtensions
{
    public static void MergeDirectory(this IFileSystem fs, string targetDir, string destDir)
    {
        targetDir = fs.Path.GetFullPath(targetDir);
        destDir = fs.Path.GetFullPath(destDir);

        var directories = fs.DirectoryInfo.FromDirectoryName(targetDir)
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Append(fs.DirectoryInfo.FromDirectoryName(targetDir))
            .OrderByDescending(x => x.FullName.Count(y => y is '/' or '\\'));

        foreach (var dir in directories)
        {
            foreach (var file in dir.EnumerateFiles())
            {
                var newPath = Regex.Replace(file.FullName, $"^{Regex.Escape(targetDir)}", destDir);
                fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(newPath));
                file.MoveTo(newPath);
            }

            dir.Delete();
        }
    }
}