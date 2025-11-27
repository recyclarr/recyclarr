using System.IO.Abstractions;

namespace Recyclarr.ConfigTemplates;

public record TemplateMetadata(string Id, IFileInfo TemplateFile, bool Hidden)
{
    public static TemplateMetadata From(TemplateEntry entry, IDirectoryInfo rootPath) =>
        new(entry.Id, rootPath.File(entry.Template), entry.Hidden);
}
