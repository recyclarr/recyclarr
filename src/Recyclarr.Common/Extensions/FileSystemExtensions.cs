using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Recyclarr.Common.Extensions;

public static class FileSystemExtensions
{
    public static void CreateParentDirectory(this IFileInfo f)
    {
        var parent = f.Directory;
        parent?.Create();
    }

    public static void CreateParentDirectory(this IFileSystem fs, string? path)
    {
        var dirName = fs.Path.GetDirectoryName(path);
        if (dirName is not null)
        {
            fs.Directory.CreateDirectory(dirName);
        }
    }

    public static void MergeDirectory(this IFileSystem fs, IDirectoryInfo targetDir, IDirectoryInfo destDir,
        IAnsiConsole? console = null)
    {
        var directories = targetDir
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Append(targetDir)
            .OrderByDescending(x => x.FullName.Count(y => y is '/' or '\\'));

        foreach (var dir in directories)
        {
            console?.WriteLine($" - Attributes: {dir.Attributes}");

            // Is it a symbolic link?
            if ((dir.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                var newPath = RelocatePath(dir.FullName, targetDir.FullName, destDir.FullName);
                fs.CreateParentDirectory(newPath);
                console?.WriteLine($" - Symlink:  {dir.FullName} :: TO :: {newPath}");
                dir.MoveTo(newPath);
                continue;
            }

            // For real directories, move all the files inside
            foreach (var file in dir.EnumerateFiles())
            {
                var newPath = RelocatePath(file.FullName, targetDir.FullName, destDir.FullName);
                fs.CreateParentDirectory(newPath);
                console?.WriteLine($" - Moving:   {file.FullName} :: TO :: {newPath}");
                file.MoveTo(newPath);
            }

            // Delete the directory now that it is empty.
            console?.WriteLine($" - Deleting: {dir.FullName}");
            dir.Delete();
        }
    }

    private static string RelocatePath(string path, string oldDir, string newDir)
    {
        return Regex.Replace(path, $"^{Regex.Escape(oldDir)}", newDir);
    }

    public static IDirectoryInfo SubDir(this IDirectoryInfo dir, params string[] subdirectories)
    {
        return subdirectories.Aggregate(dir,
            (d, s) => d.FileSystem.DirectoryInfo.New(d.FileSystem.Path.Combine(d.FullName, s)));
    }
}
