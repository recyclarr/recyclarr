using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Recyclarr.ConfigTemplates;

public partial record TemplateMetadata(string Id, IFileInfo TemplateFile, bool Hidden)
{
    public static TemplateMetadata From(TemplateEntry entry, IDirectoryInfo rootPath)
    {
        ValidateManifestId(entry.Id);
        ValidateManifestPath(entry.Template);
        return new TemplateMetadata(entry.Id, rootPath.File(entry.Template), entry.Hidden);
    }

    private static void ValidateManifestId(string id)
    {
        if (!SafeIdPattern().IsMatch(id))
        {
            throw new InvalidDataException(
                $"Template id '{id}' contains invalid characters. "
                    + "Only alphanumeric characters, underscores, and hyphens are allowed."
            );
        }
    }

    private static void ValidateManifestPath(string path)
    {
        if (Path.IsPathRooted(path) || TraversalSegmentPattern().IsMatch(path))
        {
            throw new InvalidDataException(
                $"Template path '{path}' is absolute or contains path traversal."
            );
        }
    }

    [GeneratedRegex(@"^[\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex SafeIdPattern();

    /// <summary>Matches a ".." path segment at the start, middle, or end of a path.</summary>
    [GeneratedRegex(@"(^|[\\/])\.\.($|[\\/])", RegexOptions.None, 1000)]
    private static partial Regex TraversalSegmentPattern();
}
