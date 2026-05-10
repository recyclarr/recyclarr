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

    extension(IDirectoryInfo dir)
    {
        public IFileInfo? YamlFile(string yamlFilenameNoExtension)
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

        public void RecursivelyDeleteReadOnly()
        {
            foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            dir.Delete(true);
        }

        public long DirectorySize() =>
            dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

        public void DeleteReadOnlyDirectory()
        {
            if (!dir.Exists)
            {
                return;
            }

            foreach (var subdirectory in dir.EnumerateDirectories())
            {
                subdirectory.DeleteReadOnlyDirectory();
            }

            foreach (var fileInfo in dir.EnumerateFiles())
            {
                fileInfo.Attributes = FileAttributes.Normal;
                fileInfo.Delete();
            }

            dir.Delete();
        }
    }
}
