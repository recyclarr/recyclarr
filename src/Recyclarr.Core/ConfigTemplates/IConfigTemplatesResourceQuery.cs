using System.IO.Abstractions;
using Recyclarr.TrashGuide;

namespace Recyclarr.ConfigTemplates;

public record TemplatePath
{
    public required string Id { get; init; }
    public required IFileInfo TemplateFile { get; init; }
    public required SupportedServices Service { get; init; }
    public bool Hidden { get; init; }
}

public record IncludePath
{
    public required string Id { get; init; }
    public required IFileInfo IncludeFile { get; init; }
    public required SupportedServices Service { get; init; }
}

public interface IConfigTemplatesResourceQuery
{
    IReadOnlyCollection<TemplatePath> GetTemplates();
    IReadOnlyCollection<IncludePath> GetIncludes();
}
