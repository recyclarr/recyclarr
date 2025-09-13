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

public interface IConfigTemplatesResourceQuery
{
    IReadOnlyCollection<TemplatePath> GetTemplates();
}
