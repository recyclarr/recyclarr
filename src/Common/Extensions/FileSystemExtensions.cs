using System.IO.Abstractions;
using System.Text.RegularExpressions;
using CliFx.Infrastructure;

namespace Common.Extensions;

public static class FileSystemExtensions
{
    public static void MergeDirectory(this IFileSystem fs, string targetDir, string destDir, IConsole? console = null)
    {
        targetDir = fs.Path.GetFullPath(targetDir);
        destDir = fs.Path.GetFullPath(destDir);

        var directories = fs.DirectoryInfo.FromDirectoryName(targetDir)
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Append(fs.DirectoryInfo.FromDirectoryName(targetDir))
            .OrderByDescending(x => x.FullName.Count(y => y is '/' or '\\'));

        foreach (var dir in directories)
        {
            console?.Output.WriteLine($" - Attributes: {dir.Attributes}");

            // Is it a symbolic link?
            if ((dir.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                var newPath = RelocatePath(dir.FullName, targetDir, destDir);
                fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(newPath));
                console?.Output.WriteLine($" - Symlink:  {dir.FullName} :: TO :: {newPath}");
                dir.MoveTo(newPath);
                continue;
            }

            // For real directories, move all the files inside
            foreach (var file in dir.EnumerateFiles())
            {
                var newPath = RelocatePath(file.FullName, targetDir, destDir);
                fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(newPath));
                console?.Output.WriteLine($" - Moving:   {file.FullName} :: TO :: {newPath}");
                file.MoveTo(newPath);
            }

            // Delete the directory now that it is empty.
            console?.Output.WriteLine($" - Deleting: {dir.FullName}");
            dir.Delete();
        }
    }

    private static string RelocatePath(string path, string oldDir, string newDir)
    {
        return Regex.Replace(path, $"^{Regex.Escape(oldDir)}", newDir);
    }
}
