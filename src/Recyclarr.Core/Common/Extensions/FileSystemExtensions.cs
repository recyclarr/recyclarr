using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace Recyclarr.Common.Extensions;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class FileSystemExtensions
{
    public static void CreateParentDirectory(this IFileInfo f)
    {
        var parent = f.Directory;
        parent?.Create();
    }

    public static IFileInfo? YamlFile(this IDirectoryInfo dir, string yamlFilenameNoExtension)
    {
        var supportedFiles = new[]
        {
            $"{yamlFilenameNoExtension}.yml",
            $"{yamlFilenameNoExtension}.yaml",
        };
        var configs = supportedFiles.Select(dir.File).Where(x => x.Exists).ToList();

        if (configs.Count > 1)
        {
            throw new ConflictingYamlFilesException(supportedFiles);
        }

        return configs.FirstOrDefault();
    }

    public static void RecursivelyDeleteReadOnly(this IDirectoryInfo dir)
    {
        foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            info.Attributes = FileAttributes.Normal;
        }

        dir.Delete(true);
    }

    public static void DeleteReadOnlyDirectory(this IDirectoryInfo directory)
    {
        if (!directory.Exists)
        {
            return;
        }

        foreach (var subdirectory in directory.EnumerateDirectories())
        {
            DeleteReadOnlyDirectory(subdirectory);
        }

        foreach (var fileInfo in directory.EnumerateFiles())
        {
            fileInfo.Attributes = FileAttributes.Normal;
            fileInfo.Delete();
        }

        directory.Delete();
    }
}
